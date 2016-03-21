using DotNet.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace BingImgCrawler
{
    class Program
    {
        public static Queue<string> contentQueue = new Queue<string>();
        static void Main(string[] args)
        {        
            while (true)
            {
                #region 
                Console.WriteLine("请输入关键词：");
                String keyWord = Console.ReadLine().Trim();
                keyWord = keyWord.Replace(" ", "+").Replace(",", "+").Replace("、", "+");//bing搜索，搜索条件之间用"+"连接                
                #endregion
                if (!string.IsNullOrEmpty(keyWord.Trim()))
                {
                    Thread t = new Thread(() =>
                    {
                        #region 抓取任务开始
                        String start = "";
                        String endMd5 = MD5Helper.MD5Helper.ComputeMd5String("今天天气好晴朗，又是刮风又是下雨");//因为这是一段矛盾的话，其MD5与返回值MD5一致的概率几乎为0
                        string taskKeyWord = keyWord;

                        HttpHelper http = new HttpHelper();
                        //每一次抓取
                        //大于totalItems/48时停止
                        Int32 count = 0;
                        Random random = new Random();
                        while (true)
                        {
                            start = (35 * count + 1).ToString();
                            HttpItem item = new HttpItem()
                            {
                                //URL = count > 0 ? "https://www.google.com.hk/search?q=" + taskKeyWord + "&newwindow=1&safe=strict&biw=1920&bih=995&site=imghp&tbm=isch&ijn=" + count + "&ei=NxThVtLqNKbImAXJt5n4Aw&start=" + start + "&ved=0ahUKEwiS4oX2vrXLAhUmJKYKHclbBj8QuT0IGSgB&vet=10ahUKEwiS4oX2vrXLAhUmJKYKHclbBj8QuT0IGSgB.NxThVtLqNKbImAXJt5n4Aw.i" : "http://cn.bing.com/images/search?q=" + taskKeyWord + "&qs=IM&form=QBILPG&pq=ditu&sc=8-4&sp=1&sk=",
                                // http://cn.bing.com/images/search?q=   &qs=IM&form=QBILPG&pq=ditu&sc=8-4&sp=1&sk=
                                URL = "http://cn.bing.com/images/async?q=" + taskKeyWord + "&async=content&first=" + start + "&count=35&dgst=xo_u699*ro_u625*rn_u4*rh_u135*arn_u4*ayo_u625*&IID=images.1&SFX=" + (count + 1) + "&IG=BE2EDC34B72048F09AFC75BDA5A5F101&CW=1903&CH=540&CT=1458352166236&form=QBILPG",
                                //0 1|35
                                Method = "get",//URL     可选项 默认为Get   
                                IsToLower = false,//得到的HTML代码是否转成小写     可选项默认转小写   
                                Cookie = "",//字符串Cookie     可选项   
                                Referer = "",//来源URL     可选项   
                                Postdata = "",//Post数据     可选项GET时不需要写   
                                Timeout = 100000,//连接超时时间     可选项默认为100000    
                                ReadWriteTimeout = 30000,//写入Post数据超时时间     可选项默认为30000   
                                UserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0)",//用户的浏览器类型，版本，操作系统     可选项有默认值   
                                ContentType = "text/html",//返回类型    可选项有默认值   
                                Allowautoredirect = false,//是否根据301跳转     可选项   
                                                          //CerPath = "d:\123.cer",//证书绝对路径     可选项不需要证书时可以不写这个参数   
                                                          //Connectionlimit = 1024,//最大连接数     可选项 默认为1024    
                                ProxyIp = "",//代理服务器ID     可选项 不需要代理 时可以不设置这三个参数    
                                             //ProxyPwd = "123456",//代理服务器密码     可选项    
                                             //ProxyUserName = "administrator",//代理服务器账户名     可选项   
                                ResultType = ResultType.String

                            };
                            HttpResult result = http.GetHtml(item);
                            string html = result.Html;
                            //string cookie = result.Cookie;
                            //解析结果，-----→                            

                            //List<ImgModel> list = count > 0 ? GetInfoNext(html) : GetInfoIndex(html);//Kiwi：plus
                            List<ImgModel> list = GetInfoNext(html);
                            //List<ImgModel> list = GetJsonPlus(html);
                            //解析结果，end-----→            
                            Int32 num = 0;
                            //停止条件：
                            //超过一定的值，返回相同的数据                            
                            string md5PreStr = "";
                            foreach (ImgModel model in list)
                            {
                                //*****用于判断结束******                      
                                md5PreStr += model.ImgUrl.Trim();//一个list对应一页数据，构建MD5值作为这一页数据的指纹
                                //*****用于判断结束******
                            }
                            if (MD5Helper.MD5Helper.ComputeMd5String(md5PreStr) == endMd5)
                            {
                                Console.WriteLine("结束时间：【" + DateTime.Now + "】-任务关键字：" + taskKeyWord);
                                break;
                            }
                            endMd5 = MD5Helper.MD5Helper.ComputeMd5String(md5PreStr);
                            //不重复，则存储                               
                            //存储数据
                            foreach (ImgModel model in list)
                            {
                                num++;
                                StringBuilder content = new StringBuilder();
                                content.Append("[" + taskKeyWord + "]-");
                                content.Append((count + 1).ToString() + "-" + num.ToString() + "->");
                                content.Append("→图片URL:" + model.ImgUrl);//图片地址
                                content.Append("→图片网站URL:" + model.PageUrl);//网站地址
                                content.Append("→图片Title:" + model.Title + "\r\n");
                                content.Append("→图片缩略图:" + model.ThumbnailUrl + "\r\n");
                                //contentQueue.Enqueue(content.ToString());
                                File.AppendAllText("D:/" + "bingImg" + ".txt", content.ToString());                               
                            }
                            //
                                                                                                    
                            //下一轮
                            int span = random.Next(3000, 10000);
                            Thread.Sleep(span);
                            //Console.WriteLine(count);
                            count++;                           
                        }
                        #endregion
                    });
                    //threads.Add(t);
                    t.Start();
                    Console.WriteLine("开始时间：【" + DateTime.Now + "】-任务关键字：" + keyWord);
                }
            }

        }

        /// <summary>
        /// 截取字符串中开始和结束字符串中间的字符串
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <param name="startStr">开始字符串</param>
        /// <param name="endStr">结束字符串</param>
        /// <returns>中间字符串</returns>
        public static string Substring(ref string source, string startStr, string endStr)
        {
            string result = "";
            Int32 index = source.IndexOf(startStr) + startStr.Length;
            Int32 endIndex = source.IndexOf(endStr, index);
            //string titleStr = source.Substring(index, endIndex + 1 - index);
            result = source.Substring(index, endIndex - index);
            source = source.Substring(endIndex + 1);
            return result;
        }
        /// <summary>
        /// 处理后续请求
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private static List<ImgModel> GetInfoNext(string source)
        {
            List<ImgModel> list = new List<ImgModel>();
            source = source.Replace("&quot;", "");
            while (source.Contains("class=\"dg_u\""))
            {
                ImgModel model = new ImgModel();
                model.Title = Substring(ref source, "t1=\"", "\"");
                model.PageUrl = Substring(ref source, "surl:", ",");
                model.ImgUrl = Substring(ref source, "imgurl:", ",");
                model.ThumbnailUrl = Substring(ref source, "src2=\"", "\"");
                //Title        
                //Int32 index = source.IndexOf("t1=\"") + 4;
                //Int32 endIndex = source.IndexOf("\"", index);
                //model.Title = source.Substring(index, endIndex + 1 - index);
                //string cutTitleStr = source.Substring(endIndex + 1);
                ////pageUrl
                //index = cutTitleStr.IndexOf("t3=\"") + 4;
                //endIndex = cutTitleStr.IndexOf("\"", index);
                //model.PageUrl = cutTitleStr.Substring(index, endIndex + 1 - index);
                //string cutPageUrl = cutTitleStr.Substring(endIndex + 1);
                ////imgUrl
                //index = cutPageUrl.IndexOf("imgurl:") + 7;
                //endIndex = cutPageUrl.IndexOf(";", index);
                //model.ImgUrl = cutPageUrl.Substring(index, endIndex + 1 - index);
                //string cutImgUrl = cutPageUrl.Substring(endIndex + 1);
                ////ThumbnailUrl 
                //index = cutImgUrl.IndexOf("src2=\"") + 6;
                //endIndex = cutImgUrl.IndexOf("\"", index);
                //model.ThumbnailUrl = cutImgUrl.Substring(index, endIndex + 1 - index);
                //source = cutImgUrl.Substring(endIndex);
                list.Add(model);
            }
            //string PageUrl=cutTitleStr.
            //PageUrl      
            //ImgUrl       
            //ThumbnailUrl           
            return list;
        }
        /// <summary>
        /// 处理第一次请求
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static List<ImgModel> GetInfoIndex(string source)
        {
            List<ImgModel> list = new List<ImgModel>();

            return list;
        }

    }
}
