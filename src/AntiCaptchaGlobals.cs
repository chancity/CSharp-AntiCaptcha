﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using AntiCaptcha.GetQueueStats;
using Newtonsoft.Json;

namespace AntiCaptcha
{
    public class AntiCaptchaGlobals
    {
        internal static readonly HttpClient HttpClient;
        private static readonly Timer _Qtimer;

        public static QueueIdEnum SelectedQueueStats = QueueIdEnum.RecaptchaProxyless;

        private static readonly Dictionary<QueueIdEnum, GetQueueStatsResponse> QueueStatsDictionary;
        public static int SoftId { get; set; }
        public static int CaptchaRetryLimit { get; set; }


        static AntiCaptchaGlobals()
        {
            HttpClient = new HttpClient();
            SoftId = 913;
            CaptchaRetryLimit = 10;

            QueueStatsDictionary = new Dictionary<QueueIdEnum, GetQueueStatsResponse>
            {
                {QueueIdEnum.ImageToTextEnglish, null},
                {QueueIdEnum.ImageToTextRussian, null},
                {QueueIdEnum.RecaptchaProxyless, null},
                {QueueIdEnum.RecaptchaNoCaptcha, null}
            };

            _Qtimer = new Timer(1000);
            _Qtimer.Elapsed += QtimerOnElapsed;
            _Qtimer.Enabled = true;
        }

        public static GetQueueStatsResponse GetStatsForSelectedQueue()
        {
            lock (QueueStatsDictionary)
            {
                return QueueStatsDictionary.ContainsKey(SelectedQueueStats)
                    ? QueueStatsDictionary[SelectedQueueStats]
                    : null;
            }
        }

        private static void QtimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            _Qtimer.Stop();

            try
            {
                Array values = Enum.GetValues(typeof(QueueIdEnum));

                foreach (object queueIdEnum in values)
                {
                    QueueStatsDictionary[(QueueIdEnum) queueIdEnum] = GetQStats((QueueIdEnum) queueIdEnum).Result;
                }
            }
            catch
            {
                //ignored
            }

            _Qtimer.Start();
        }

        private static async Task<GetQueueStatsResponse> GetQStats(QueueIdEnum queueId)
        {
            string request = JsonConvert.SerializeObject(new GetQueueStatsRequest(queueId));

            using (HttpRequestMessage httpRequest =
                new HttpRequestMessage(HttpMethod.Post, AntiCaptchaEndpoints.GetQueueStatsUrl))
            {
                httpRequest.Content = new StringContent(request, Encoding.UTF8, "application/json");

                using (HttpResponseMessage httpResponse = await HttpClient.SendAsync(httpRequest))
                {
                    httpResponse.EnsureSuccessStatusCode();
                    string value = await httpResponse.Content.ReadAsStringAsync();
                    GetQueueStatsResponse ret = JsonConvert.DeserializeObject<GetQueueStatsResponse>(value);
                    return ret;
                }
            }
        }
    }
}