using AutoLeigodPauseRobot.Dialog;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoLeigodPauseRobot
{
    class Config
    {
        public string PhoneCountryNumber { get; set; } = "86";
        public string PhoneNumber { get; set; }
        public string Password { get; set; }
    }

    public class Program
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);
        
        const string CONFIG_FILE = "config.json";
        const string PAUSE_API = @"https://webapi.leigod.com/api/user/pause";
        const string LOGIN_API = @"https://webapi.leigod.com/api/auth/login";

        private static Config config;

        private static MD5 md5 = MD5.Create();

        private static NotifyIcon notify_icon;

        private static Thread timer;

        public static void Main(string[] args)
        {
            LoadOrInitConfig();

            InitStrayAndHideConsole();

            InitWorkThread();

            while (true)
            {
                Application.DoEvents();
            }
        }

        private static void InitWorkThread()
        {
            timer=new Thread(OnClientCheck);
            timer.Start();

            notify_icon.ShowBalloonTip(5000, "AutoLeigodPauseRobot", "开始自动检测...", ToolTipIcon.Info);
        }

        private static void OnClientCheck()
        {
            bool client_running = false;

            while (true)
            {
                var client_proc_exist = Process.GetProcessesByName("leigod").Any();

                if (!client_proc_exist&&client_running)
                {
                    PostPauseRequest();
                }

                client_running=client_proc_exist;

                Thread.Sleep(1000);
            }
        }

        private static void PostPauseRequest()
        {
            try
            {
                HttpWebRequest request = BuildCommonHttpPostRequest(LOGIN_API);

                var md5_pw=string.Join("",md5.ComputeHash(Encoding.Default.GetBytes(config.Password)).Select(x=>x.ToString("X2"))).ToLower();

                var body = new {
                    username = config.PhoneNumber,
                    password = md5_pw,
                    usertype = "0",
                    src_channel = "guanwang",
                    code = "",
                    country_code = config.PhoneCountryNumber,
                    lang = "zh_CN"
                };

                var body_str = JsonConvert.SerializeObject(body);

                using (var stream=new StreamWriter(request.GetRequestStream()))
                {
                    stream.Write(body_str);
                }

                var response = request.GetResponse();


                using (var stream = new StreamReader(response.GetResponseStream()))
                {
                    var result = JsonConvert.DeserializeObject(stream.ReadToEnd()) as JObject;

                    if (result["code"].ToObject<int>()==0)
                    {
                        var account_token = result["data"]["login_info"]["account_token"].ToString();

                        request=BuildCommonHttpPostRequest(PAUSE_API);

                        var body2 = new
                        {
                            account_token,
                            lang = "zh_CN"
                        };

                        body_str=JsonConvert.SerializeObject(body2);

                        using (var stream2 = new StreamWriter(request.GetRequestStream()))
                        {
                            stream2.Write(body_str);
                        }

                        var response2 = request.GetResponse();

                        using (var stream3 = new StreamReader(response2.GetResponseStream()))
                        {
                            var result2 = JsonConvert.DeserializeObject(stream3.ReadToEnd()) as JObject;

                            var code = result2["code"].ToObject<int>();

                            if (code==0||code==400803)
                            {
                                Console.WriteLine("暂停成功");
                                notify_icon.ShowBalloonTip(5000, "AutoLeigodPauseRobot", "自动暂停成功!", ToolTipIcon.Info);
                            }
                            else
                                throw new Exception(result["msg"].ToString());
                        }
                    }
                    else
                        throw new Exception(result["msg"].ToString());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("暂停失败:"+e.Message);
            }
        }

        private static HttpWebRequest BuildCommonHttpPostRequest(string url)
        {
            var request=WebRequest.CreateHttp(url);
            request.ContentType=@"application/json;charset=UTF-8";
            request.Referer=@"https://user.leigod.com/login.html";
            request.UserAgent=@"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/68.0.3440.106 Safari/537.36";
            request.Method="POST";

            return request;
        }

        private static void InitStrayAndHideConsole()
        {
            notify_icon=new NotifyIcon();
            notify_icon.Visible=true;
            notify_icon.Icon=Properties.Resources.Icon1;

#if RELEASE
            var console_window_handler=FindWindow("ConsoleWindowClass", Console.Title);
            ShowWindow(console_window_handler, 0);
#endif
        }

        private static void LoadOrInitConfig()
        {
            try
            {
                if (!File.Exists(CONFIG_FILE))
                    InitConfig();

                config=JsonConvert.DeserializeObject<Config>(File.ReadAllText(CONFIG_FILE));

                if (config==null && string.IsNullOrWhiteSpace(config.PhoneNumber))
                    InitConfig();
            }
            catch (Exception e)
            {
                Console.WriteLine($"加载{CONFIG_FILE}文件错误({e.Message})，请重新输入账号密码.");

                InitConfig();
            }
        }

        private static void InitConfig()
        {
            if (config==null)
                config=new Config();

            InputDialog dialog;

            if (string.IsNullOrWhiteSpace(config.PhoneNumber))
            {
                dialog=new InputDialog("请输入手机号码:", false);
                dialog.ShowDialog();

                config.PhoneNumber=dialog.InputText;
            }


            if (string.IsNullOrWhiteSpace(config.Password))
            {
                dialog=new InputDialog("请输入密码:", true);
                dialog.ShowDialog();

                config.Password=dialog.InputText;
            }

            try
            {
                File.WriteAllText(CONFIG_FILE,JsonConvert.SerializeObject(config));
                Console.WriteLine("保存配置文件成功!:");
            }
            catch (Exception e)
            {
                Console.WriteLine("保存配置文件失败!:"+e.Message);
            }
        }
    }
}
