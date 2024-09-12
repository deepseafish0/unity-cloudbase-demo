using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Demo
{
    public class DemoSDK : MonoBehaviour
    {
        private static readonly Lazy<DemoSDK> _instance = new Lazy<DemoSDK>(() =>
        {
            GameObject gameObject = new("DemoSDK");
            DemoSDK demoSDK = gameObject.AddComponent<DemoSDK>();
            return demoSDK;
        });
        public static DemoSDK Instance => _instance.Value;
        private DemoSDK() { }

        private Dictionary<string, TaskCompletionSource<string>> tcsDictionary = new();

        public void Hello()
        {
            Internal.Hello();
        }

        public void HelloWithInput(HelloWithInputParams input)
        {
            Internal.HelloWithInput(Internal.ParseInputParams(input));
        }

        public HelloWithReturnResult HelloWithReturn()
        {
            return Internal.ParseOutputResult<HelloWithReturnResult>(Internal.HelloWithReturn());
        }

        public void HelloCallOtherFn()
        {
            Internal.HelloCallOtherFn();
        }

        public void WXLogin()
        {
            Internal.WXLogin();
        }

        public async Task<HelloWithReturnResult> HelloAsyncFn(HelloWithInputParams input)
        {
            (string, TaskCompletionSource<string>) asyncTask = GetAsyncTask();

            // 调用 JavaScript 方法
            Internal.HelloAsyncFn(asyncTask.Item1, Internal.ParseInputParams(input));

            // 返回 Task 让调用方等待
            var result = await asyncTask.Item2.Task;
            return Internal.ParseOutputResult<HelloWithReturnResult>(result);
        }

        /**
         * JavaScript 异步方法回调调用的方法
         * 注意：必须是这个名字，配合 tcbsdk.jslib，请不要修改
         */
        public void OnAsyncFnCompleted(string result)
        {
            AsyncResponse<string> res = Internal.ParseOutputResult<AsyncResponse<string>>(result);

            tcsDictionary[res.callbackId].SetResult(res.result);
            // FIXME: 这一句是为了让整个回调能够正常执行，但原因不明，也很奇怪，后面再深入了解
            Task.Factory.StartNew(() => { });
            tcsDictionary.Remove(res.callbackId);
        }

        private (string, TaskCompletionSource<string>) GetAsyncTask()
        {
            string uuid = Guid.NewGuid().ToString();
            TaskCompletionSource<string> tcs = new();
            tcsDictionary.Add(uuid, tcs);
            return (uuid, tcs);
        }

        private class Internal
        {
            [DllImport("__Internal")]
            public static extern void Hello();

            [DllImport("__Internal")]
            public static extern void HelloWithInput(string input);

            [DllImport("__Internal")]
            public static extern string HelloWithReturn();

            [DllImport("__Internal")]
            public static extern void HelloCallOtherFn();

            [DllImport("__Internal")]
            public static extern void HelloAsyncFn(string callbackId, string input);

            [DllImport("__Internal")]
            public static extern void WXLogin();

            public static string ParseInputParams<T>(T InputParams)
            {
                return JsonConvert.SerializeObject(InputParams);
            }

            public static T ParseOutputResult<T>(string output)
            {
                return JsonConvert.DeserializeObject<T>(output);
            }
        }

        private class AsyncResponse<T>
        {
            public string callbackId { get; set; }
            public T result { get; set; }
        }
    }


    public class HelloWithInputParams
    {
        public string name { get; set; }
    }

    public class HelloWithReturnResult
    {
        public string name { get; set; }
    }


}
