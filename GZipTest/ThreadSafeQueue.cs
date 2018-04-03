using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace GZipTest
{
    public class ThreadSafeQueue<T>
    {
        private readonly object criticalSection = new object();
        private static Semaphore emptyCountSemaphore;
        private static Semaphore fillCountSemaphore;

        Queue<T> queue = new Queue<T>();

        public ThreadSafeQueue(int length)
        {
            if (length <= 0)
                throw new ArgumentException(nameof(length));

            emptyCountSemaphore = new Semaphore(length, length);
            fillCountSemaphore = new Semaphore(0, length);
        }

        public void Enqueue(T o)
        {
            emptyCountSemaphore.WaitOne();

            lock (criticalSection)
                queue.Enqueue(o);

            fillCountSemaphore.Release();
        }

        public T Dequeue()
        {
            fillCountSemaphore.WaitOne();
            T item;

            lock (criticalSection)
                item = queue.Dequeue();

            emptyCountSemaphore.Release();
            return item;
        }
    }
}
