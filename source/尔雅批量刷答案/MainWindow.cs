using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Web;
using System.Net;
using System.IO;
using System.Xml;
using System.Runtime.InteropServices;

namespace 尔雅批量刷答案
{
    public partial class MainWindow : Form
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        //重绘窗口的Windows API
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static public extern bool UpdateWindow(IntPtr hWnd);

        /// <summary>
        /// 返回剪切板的内容
        /// </summary>
        /// <returns>剪切板有文本则返回文本，否则返回空字符串</returns>
        public static string getClipboardText()
        {
            //获取剪切板
            IDataObject iData = Clipboard.GetDataObject();
            //如果内容是HTML或者TEXT，返回
            if (iData.GetDataPresent(DataFormats.Html) || iData.GetDataPresent(DataFormats.Text))
            {
                return (String)iData.GetData(DataFormats.Text);
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// 返回从尔雅题库取得的答案
        /// </summary>
        /// <param name="keyword"></param>
        /// <returns>字符串，多行的答案</returns>
        private static string getAnswer(string keyword)
        {
            try
            {
                HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create("http://erya.hang.im/search/");
                myRequest.Method = "POST";
                //经测试，必须指定Host否则返回HTTP 500
                myRequest.Host = "erya.hang.im";
                //伪装火狐
                myRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:33.0) Gecko/20100101 Firefox/33.0";
                myRequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                myRequest.Referer = "http://erya.hang.im/";


                //参数经过URL编码
                string paraUrlCoded = "keyword";
                //构造请求主体
                paraUrlCoded += "=" + System.Web.HttpUtility.UrlEncode(keyword);
                byte[] payload;
                //将URL编码后的字符串转化为字节
                payload = System.Text.Encoding.UTF8.GetBytes(paraUrlCoded);

                myRequest.ContentLength = payload.Length;

                Stream newStream = myRequest.GetRequestStream();
                //发送
                newStream.Write(payload, 0, payload.Length);
                newStream.Close();

                //获得请求响应
                HttpWebResponse myResponse = (HttpWebResponse)myRequest.GetResponse();
                StreamReader reader = new StreamReader(myResponse.GetResponseStream(), Encoding.UTF8);

                //声明要返回的字符串
                string returnText = "";

                try
                {
                    XmlDocument xm = new XmlDocument();
                    xm.LoadXml(reader.ReadToEnd());
                    XmlNodeList listNodes = xm.SelectNodes("/ul/li/p");
                    int lineCount = 0;

                    foreach (XmlNode xmNode in listNodes)
                    {
                        lineCount++;
                        //xmNode.Name
                        if (lineCount % 2 == 0)
                        {
                            //如果是双数行，可能是答案行
                            if (xmNode.InnerText == "正确")
                            {
                                returnText += "【√】 ■";
                            }
                            else if (xmNode.InnerText == "错误")
                            {
                                returnText += "【×】 □";
                            }
                            else
                            {
                                returnText += "    ■ ";
                            }
                        }
                        returnText += xmNode.InnerText;
                        returnText += Environment.NewLine;
                    }
                }
                catch (XmlException)
                {
                    return "错误：从服务器返回的数据无法解析，可能是校园网未登录";
                }

                if (returnText == "")
                {
                    returnText = "卧槽居然没有答案" + Environment.NewLine;
                }

                return returnText;
            }
            catch(WebException)
            {
                return "网络连接中断";
            }
        }

        /// <summary>
        /// 核心函数，从剪切板读题目，获取答案并显示
        /// </summary>
        public void startSearch()
        {
            //读入剪切板
            string inputText = getClipboardText();

            if (inputText.Trim() != "")
            {
                outputBox.Text = "";
                //匹配题目的正则表达式
                string regexText = "\\d+、.*\\s[(]\\d{1,3}\\.\\d{1,2}分[)]";
                //如果能匹配到题目
                if (Regex.IsMatch(inputText, regexText))
                {
                    //存放所有题目的集合
                    MatchCollection titles = Regex.Matches(inputText, regexText);
                    //遍历数组操作每一题
                    foreach (Match title in titles)
                    {
                        //提取题目序号
                        string orderNumber = Regex.Match(title.Value, "\\d+、").Value;
                        //提取题目核心文本
                        string searchText = Regex.Replace(title.Value, "\\d+、", "");
                        searchText = Regex.Replace(searchText, "[(]\\d{1,3}\\.\\d{1,2}分[)]", "");
                        //删首尾空格
                        searchText = searchText.Trim();
                        outputBox.Text += "题号：" + orderNumber + "-----------------" + Environment.NewLine;
                        outputBox.Text += getAnswer(searchText) + Environment.NewLine;
                        
                        //刷新自身窗口
                        UpdateWindow(this.Handle);
                    }
                }
                else
                {
                    //如果剪切板的内容不包含题目文本
                    outputBox.Text = "_(:3」∠)_卧槽为什么没有题目！！" +
                        Environment.NewLine + Environment.NewLine +
                        "剪切板中需要格式正确的题目" +
                        Environment.NewLine + Environment.NewLine +
                        "请用谷歌浏览器直接复制所有题目(=・ω・=)";
                }
            }
            else
            {
                //如果剪切板的内容是空格、空白或者非文字
                outputBox.Text = "你的剪切板里没有文字=-=...";
            }
        }

        private void searchButton_Click(object sender, EventArgs e)
        {
            startSearch();
        }

        private void fontButton_Click(object sender, EventArgs e)
        {
            //改变字体
            FontDialog fontDialog = new FontDialog();
            fontDialog.Font = outputBox.Font;
            if (fontDialog.ShowDialog() != DialogResult.Cancel)
            {
                outputBox.Font = fontDialog.Font;  
            }
        }
    }
}
