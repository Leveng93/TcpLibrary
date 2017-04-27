using System;
using System.Threading;
using System.Threading.Tasks;

namespace TcpLibrary
{
    public static class TaskExtensions
    {
        public static async Task<T> WithWaitCancellation<T>(this Task<T> task, CancellationToken cancellationToken) 
        {
            // http://www.eqianli.tech/questions/171784/what-is-the-correct-way-to-cancel-an-async-operation-that-doesnt-accept-a-cance

            // The tasck completion source. 
            var tcs = new TaskCompletionSource<bool>(); 

            // Register with the cancellation token.
            using(cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs)) 
                // If the task waited on is the cancellation token...
                if (task != await Task.WhenAny(task, tcs.Task)) 
                    throw new OperationCanceledException(cancellationToken); 

            // Wait for one or the other to complete.
            return await task; 
        }
    }
}