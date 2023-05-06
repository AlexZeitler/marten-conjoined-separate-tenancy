using Marten;
using Marten.Events;
using Marten.Internal;
using Marten.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MartenConjoinedSeparateTenancy.Tests.Helpers;

public class PollingMartenEventListener<T> : IDocumentSessionListener where T : IDocumentStore
{
  private readonly ILogger _logger;
  readonly List<IEvent> _events = new();
  readonly List<object> _projections = new();

  public PollingMartenEventListener(
    ILogger logger
  )
  {
    _logger = logger;
  }

  public PollingMartenEventListener()
  {
    _logger = NullLogger.Instance;
  }

  public Task AfterCommitAsync(
    IDocumentSession session,
    IChangeSet commit,
    CancellationToken token
  )
  {
    _projections.AddRange(commit.Updated);
    var events = commit.GetEvents();
    _logger.LogInformation($"AfterCommitAsync Listener collected {events.Count()} events");
    _events.AddRange(events);

    return Task.CompletedTask;
  }

  public void BeforeSaveChanges(
    IDocumentSession session
  )
  {
  }

  public Task BeforeSaveChangesAsync(
    IDocumentSession session,
    CancellationToken token
  )
  {
    return Task.CompletedTask;
  }

  public void AfterCommit(
    IDocumentSession session,
    IChangeSet commit
  )
  {
  }

  public void DocumentLoaded(
    object id,
    object document
  )
  {
  }

  public void DocumentAddedForStorage(
    object id,
    object document
  )
  {
  }

  public Task WaitForProjection<T>(
    Func<T, bool> predicate,
    CancellationToken? token = default
  )
  {
    _logger.LogInformation($"Listener waiting for Projection {typeof(T)}");

    void Check(
      CancellationToken token
    )
    {
      var from = 0;
      var attempts = 1;
      while (!token.IsCancellationRequested)
      {
        _logger.LogInformation($"Looking for expected projection - attempt #{attempts}");
        var upTo = _projections.Count;

        for (var index = from; index < upTo; index++)
        {
          var ev = _projections[index];

          if (typeof(T) == ev.GetType() && predicate((T)ev))
          {
            _logger.LogInformation($"Listener Found Projection {typeof(T).Name} with Id: {((dynamic)ev).Id}");
            return;
          }
        }

        from = upTo;

        Thread.Sleep(200);
        attempts++;
      }
    }

    var cts = new CancellationTokenSource();
    cts.CancelAfter(TimeSpan.FromSeconds(10));

    var t = token ?? cts.Token;

    return Task.Run(() => Check(t), t);
  }

  public Task WaitForEvent<T>(
    Func<T, bool> predicate,
    CancellationToken? token = default
  )
  {
    _logger.LogInformation("Listener waiting for event");

    void Check(
      CancellationToken cancel
    )
    {
      var from = 0;
      var attempts = 1;

      while (!cancel.IsCancellationRequested)
      {
        _logger.LogInformation($"Looking for expected event - attempt #{attempts}");
        var upTo = _events.Count;

        for (var index = from; index < upTo; index++)
        {
          var ev = _events[index];

          if (typeof(T).IsAssignableFrom(ev.EventType) && predicate((T)ev.Data))
          {
            _logger.LogInformation("Listener found the event");
            _logger.LogInformation($"Found Event stream id: {ev.StreamId}");
            return;
          }
        }

        from = upTo;

        Thread.Sleep(200);
        attempts++;
      }

      cancel.ThrowIfCancellationRequested();
    }

    var cts = new CancellationTokenSource();
    cts.CancelAfter(TimeSpan.FromSeconds(10));

    var t = token ?? cts.Token;

    return Task.Run(() => Check(t), t);
  }
}

public class MartenEventListenerConfig<T> : IConfigureMarten<T> where T : IDocumentStore
{
  public void Configure(
    IServiceProvider services,
    StoreOptions options
  )
  {
    var listener = services.GetService<PollingMartenEventListener<T>>();
    options.Listeners.Add(listener);
    options.Projections.AsyncListeners.Add(listener);
  }
}
