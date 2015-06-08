using AnsyncLib.Core;
using AnsyncLib.Redis;
using ServerCommon.Context;
using ServerCommon.Web.Model;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace TestService45.Controllers
{
    public class ValuesController : ApiController
    {
        // GET api/values
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        [HttpGet]
        public BaseResponse<String> Ping(string key = null)
        {
            MyRequestContext.Current = new MyRequestContext();
            return new BaseResponse<String>(key, MyRequestContext.Current);
        }

        private static ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("10.0.1.3");
        [HttpGet]
        public BaseResponse<String> StringGet(string key = null)
        {
            MyRequestContext.Current = new MyRequestContext();
            var db = redis.GetDatabase();
            String value = null;
            if (key != null)
            {
                value = db.StringGet(key);
            }
            return new BaseResponse<String>(value, MyRequestContext.Current);
        }

        [HttpGet]
        public BaseResponse<String> StringGetCount(string key = null, int count = 100)
        {
            MyRequestContext.Current = new MyRequestContext();
            var db = redis.GetDatabase();
            String value = null;
            if (key != null)
            {
                for (int i = 0; i < count; i++)
                {
                    value = db.StringGet(key);
                }
            }
            return new BaseResponse<String>(value, MyRequestContext.Current);
        }

        [HttpGet]
        public async Task<BaseResponse<String>> StringGetCountAnsync(string key = null, int count = 100)
        {
            MyRequestContext.Current = new MyRequestContext();
            var db = redis.GetDatabase();
            String value = null;
            if (key != null)
            {
                for (int i = 0; i < count; i++)
                {
                    value = await db.StringGetAsync(key);
                }
            }
            return new BaseResponse<String>(value, MyRequestContext.Current);
        }

        [HttpGet]
        public Task<BaseResponse<String>> StringGetCountJob(string key = null, int count = 100)
        {
            MyRequestContext.Current = new MyRequestContext();
            var db = redis.GetDatabase();

            var source = new TaskCompletionSource<BaseResponse<String>>();
            var job = new MyLoopGetJob(db, key, count);
            MyRequestContext ctx = MyRequestContext.Current;
            job.Start((o, e) =>
            {
                source.SetResult(new BaseResponse<String>(o[0], ctx));
            });

            return source.Task;
        }
    }

    public class MyLoopGetJob : IAnsyncJob<List<string>>
    {
        private int maxCount;
        private IDatabase db;
        private String key;
        private List<String> results = new List<string>();
        public MyLoopGetJob(IDatabase db, String key, int count)
        {
            this.db = db;
            this.key = key;
            this.maxCount = count;
        }

        private AnsyncJobCallback<List<string>> finalResultHandler;
        public void Start(AnsyncJobCallback<List<string>> handler)
        {
            this.finalResultHandler = handler;
            Console.WriteLine(" event=start thread=" + Thread.CurrentThread.ManagedThreadId);
            db.StringGet(key, this.Step1);
        }

        private void Step1(String value, Exception e)
        {
            //Console.WriteLine(" thread=" + Thread.CurrentThread.ManagedThreadId + " loop=" + results.Count + " value=" + value + " guid=" + MyRequestContext.Current.requestId);
            results.Add(value);
            if (results.Count >= maxCount)
            {
                finalResultHandler(results, null);
            }
            else
            {
                db.StringGet(key, this.Step1);
            }
        }
    }

}
