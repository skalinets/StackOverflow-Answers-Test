using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Xunit;
using FluentAssertions;

namespace StackOverflow
{
    /// <summary>
    /// http://stackoverflow.com/questions/8667819/nullreferenceexception-reading-from-an-asynchronous-httpwebrequest-stream
    /// </summary>
    public class nullref_from_async_web_stream_8667819
    {
        private Stream postStream;
        private StreamReader streamResponse;
        private string sUri = "http://yahoo.com";
        private ManualResetEvent e = new ManualResetEvent(false);
        private CountdownEvent countdownEvent;

        [Fact]
        public void Test()
        {
            var request = (HttpWebRequest) WebRequest.Create(sUri);
            request.Method = "post";
            request.BeginGetRequestStream(GetRequestStreamCallback, request);
            var success = e.WaitOne(5000);

            success.Should().BeTrue();

        }

        [Fact(/*Timeout = 5000*/)]
        public void Test_ManyThreads()
        {
            var initialCount = 20;
            countdownEvent = new CountdownEvent(initialCount);
            Enumerable.Repeat(1, initialCount).Select(_ =>
                                                 {
                                                     var request = (HttpWebRequest) WebRequest.Create(sUri);
                                                     request.Method = "post";
                                                     return request.BeginGetRequestStream(GetRequestStreamCallback, request);
                                                 }).ToList();
            var success = countdownEvent.WaitHandle.WaitOne();

            success.Should().BeTrue();

        }

//The request callback method:

        private void GetRequestStreamCallback(IAsyncResult asynchronousResult)
        {
            var request = (HttpWebRequest) asynchronousResult.AsyncState;
            postStream = request.EndGetRequestStream(asynchronousResult);

//  this.bSyncOK = Send(); //This is my method to send data to the server
            postStream.Close();

//  if (this.bSyncOK)
            request.BeginGetResponse(GetResponseCallback, request);
//  else
//    manualEventWait.Set(); //This ManualResetEvent notify a thread the end of the communication, then a progressbar must be hidden
        }

//The response callback method:

        private void GetResponseCallback(IAsyncResult asynchronousResult)
        {
            var request = (HttpWebRequest) asynchronousResult.AsyncState;
            using (var response = (HttpWebResponse) request.EndGetResponse(asynchronousResult))
            {
                using (streamResponse = new StreamReader(response.GetResponseStream()))
                {
                    Recv(); //This is my generic method to receive the data
                    streamResponse.Close();
                }
                response.Close();
            }
            e.Set();
            countdownEvent.Signal();
        }

//And finally, this is the code where I get the exception reading the stream data:
        private void Recv()
        {
            int iBytesLeidos;
            var byteArrayUTF8 = new byte[8];
            iBytesLeidos = streamResponse.BaseStream.Read(byteArrayUTF8, 0, 8); //NullReferenceException!!! -Server always send 8 bytes 
            Console.Out.WriteLine("iBytesLeidos = {0}", iBytesLeidos);
        }
    }
}