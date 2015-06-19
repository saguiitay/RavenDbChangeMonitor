# RavenDbChangeMonitor

RavenDb provides a push notifications mechanism, named [Changes API](http://ravendb.net/docs/article-page/3.0/csharp/client-api/changes/what-is-changes-api), that
allows you to receive messages from a server about events that occurred there. This mechanism makes is extremely simple to create
a [ChangeMonitor](https://msdn.microsoft.com/en-us/library/system.runtime.caching.changemonitor(v=vs.110).aspx) class that can be
used with [CacheItemPolicy](https://msdn.microsoft.com/en-us/library/system.runtime.caching.cacheitempolicy(v=vs.110).aspx), in order to expire cache items when
something happens on the server (such as a document being updated, and index is modified, etc.)

We'll start be creating our basic ChangeMonitor class with some boilerplate code:

    public class RavenDbChangeMonitor<T> : ChangeMonitor
        where T : EventArgs
    {
        public RavenDbChangeMonitor()
        {
            UniqueId = Guid.NewGuid().ToString();
      		  
    		    // TODO: Implement monitoring logic
            
            InitializationComplete();
        }
          
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
			          // TODO: Stop monitoring
            }
        }

        public override string UniqueId { get; }
    }

After that, we'll create a private Observer class, which we'll use to subscribe to the RavenDb changes:

    private class Observer : IObserver<T>
    {
        private readonly RavenDbChangeMonitor<T> _monitor;
    
        public Observer(RavenDbChangeMonitor<T> monitor)
        {
            _monitor = monitor;
        }
    
        public void OnNext(T value)
        {
            _monitor.OnChanged(value);
        }
    
        public void OnError(Exception error) { }
    
        public void OnCompleted() { }
    }

Now, all we need to do is get the subscriptions, and register our Observer. Let's add a field and change the constructor of RavenDbChangeMonitor:

    private readonly List<IDisposable> _subscribtions = new List<IDisposable>();

    public RavenDbChangeMonitor(params IObservableWithTask<T>[] observableWithTasks)
    {
        UniqueId = Guid.NewGuid().ToString();
	      // Create our observer
        var observer = new Observer(this);
    
	      // For each observable, we'll subscribe and keep the subscription for later usage
        foreach (var observableWithTask in observableWithTasks)
        {
            var subscription = observableWithTask.Subscribe(observer);
            _subscribtions.Add(subscription);
        }
    
        InitializationComplete();
    }

Lastly, we need to make sure to unsubscribe from the notifications on dispose:

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var subscription in _subscribtions)
                subscription.Dispose();
        }
    }

We can now use our ChangeMonitor with a MemoryCache:

    // Register to get notifications on changes of document with Id "documents/1"
    var changes = store.Changes().ForDocument("documents/1");

    // Create a cache policy, and add our RavenDbChangeMonitor
    CacheItemPolicy policy = new CacheItemPolicy();
    policy.ChangeMonitors.Add(new RavenDbChangeMonitor.RavenDbChangeMonitor<DocumentChangeNotification>(changes));

    // Add entry to cache
    cache.Set("cacheKey", "cacheValue", policy);
