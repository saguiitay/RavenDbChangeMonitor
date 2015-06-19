using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using Raven.Client.Changes;

namespace RavenDbChangeMonitor
{
    public class RavenDbChangeMonitor<T> : ChangeMonitor
        where T : EventArgs
    {
        private readonly List<IDisposable> _subscribtions = new List<IDisposable>();

        public RavenDbChangeMonitor(params IObservableWithTask<T>[] observableWithTasks)
        {
            UniqueId = Guid.NewGuid().ToString();
            var observer = new Observer(this);

            foreach (var observableWithTask in observableWithTasks)
            {
                var subscription = observableWithTask.Subscribe(observer);
                _subscribtions.Add(subscription);
            }

            InitializationComplete();
        }

        #region Overrides of ChangeMonitor

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var subscription in _subscribtions)
                    subscription.Dispose();
            }
        }

        public override string UniqueId { get; }

        #endregion

        private class Observer : IObserver<T>
        {
            private readonly RavenDbChangeMonitor<T> _monitor;

            public Observer(RavenDbChangeMonitor<T> monitor)
            {
                _monitor = monitor;
            }

            #region Implementation of IObserver<in DocumentChangeNotification>

            public void OnNext(T value)
            {
                _monitor.OnChanged(value);
            }

            public void OnError(Exception error) { }

            public void OnCompleted() { }

            #endregion
        }
    }
}
