using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using HtmlAgilityPack;
using System.Xml;
using System.Xml.Linq;
using System.ComponentModel;

namespace moneyMaker.download
{
    /// <summary>
    /// download_daily_trading_value.xaml 的互動邏輯
    /// 下載上市上櫃公司每日交易報表
    /// 台灣證券交易所的資料最早從民國82年開始 1993年
    /// 證券櫃檯買賣中心 最早從民國83年1月開始 1994/1
    /// 
    /// 程式流程:
    /// 1.跑security_code_information中的每一個個股代號
    /// 2.再去網路上下載該代號的資訊存在stock_database中
    /// 3.儲存資料後清空stock_database
    /// </summary>
    public partial class download_daily_trading_value : UserControl
    {
        char debug_level = '1';
        string file_path;
        string company_type;//上櫃公司就用OTC 上市公司用public
        string security_code;
        DateTime start_date;
        DateTime end_date = DateTime.Today;
        Dictionary<string, daily_trading_value_content> stock_database = new Dictionary<string, daily_trading_value_content>();

        public download_daily_trading_value()
        {
            InitializeComponent();
        }

        public void initial()
        {
            Debug.error(this, debug_level, "initial");

            get_last_update_time();
            download_all();
            save_update_time();

            Debug.error(this, debug_level, "initial X");
        }

        //取得上次更新日期
        public void get_last_update_time()
        {
            XDocument xml = XDocument.Load(Path.data_config_file_path);
            var linq_date = from linq in xml.Descendants()
                             where linq.Name.LocalName == "download_daily_trading_value_last_update_time"
                             select new
                             {
                                 date_linq = linq.Attribute("time")
                             };

            foreach (var result in linq_date)
            {
                start_date = DateTime.Parse(result.date_linq.Value);          
            }
        }

        //儲存更新資料的時間
        public void save_update_time()
        {
            XDocument xml = XDocument.Load(Path.data_config_file_path);
            var linq_date = from linq in xml.Descendants()
                            where linq.Name.LocalName == "download_daily_trading_value_last_update_time"
                            select new
                            {
                                date_linq = linq.Attribute("time")
                            };

            foreach (var result in linq_date)
            {
                result.date_linq.SetValue(DateTime.Today.ToShortDateString());
            }
            xml.Save(Path.data_config_file_path);
        }

        //下載單一上市公司個股資料 
        public void download_single_stock_data()
        {
            get_last_update_time();

            if (4 == textBox1.Text.Length)
            {
                string key = textBox1.Text;
                string market_type = stock_dictionary.security_code_information[key].market_type;
                security_code = textBox1.Text;
                if (market_type == "上市")
                {
                    update_stock_information();
                }
                else if (market_type == "上櫃")
                {
                    update_OTC_stock_information();
                }
            }
        }
       
        //下載全部公司資料
        public void download_all()
        {
            Debug.error(this, debug_level, "download_all");
            Debug.error(this, debug_level, "準備更新筆數:" + stock_dictionary.security_code_information.Count);
            for (int i = 0; i < stock_dictionary.security_code_information.Count; i++)
            {
                security_code = stock_dictionary.security_code_information.ElementAt(i).Key;
                string market_type = stock_dictionary.security_code_information.ElementAt(i).Value.market_type;
                if (market_type == "上市")
                {
                    update_stock_information();
                }
                else if (market_type == "上櫃")
                {
                    update_OTC_stock_information();
                }
                else
                {
                    Debug.error(this, debug_level, "market_type error!!");
                }
                Debug.error(this, debug_level, "完成security_code " + security_code + " 進度" + (i + 1) + "/" + stock_dictionary.security_code_information.Count);
            }
            Debug.error(this, debug_level, "download_all X");
        }

        public void update_stock_information()
        {
            company_type = "public";

            file_path = Path.daily_trading_value_path + security_code + ".xml";

            if (false == File.Exists(file_path))//如果該個股xml檔案不存在
            {
                creat_stock_date();
            }
            else//若該個股存在，就做更新的動作
            {
                //更新到今天
                update_stock_date();
            }
        }
        public void update_OTC_stock_information()
        {
            company_type = "OTC";

            file_path = Path.daily_trading_value_path + security_code + ".xml";

            if (false == File.Exists(file_path))//如果該個股xml檔案不存在
            {
                creat_stock_date();
            }
            else//若該個股存在，就做更新的動作
            {
                //更新到今天
                update_stock_date();
            }
        }
        public void grep_daily_trading_data(DateTime search_date)
        {
            int num = 0;
            //http://www.twse.com.tw/ch/trading/exchange/STOCK_DAY/genpage/Report200910/200910_F3_1_8_2342.php?STK_NO=2342&myear=2009&mmon=10
            string url = "http://www.twse.com.tw/ch/trading/exchange/STOCK_DAY/genpage/Report" + search_date.Year.ToString() + search_date.Month.ToString("00") + "/" + search_date.Year.ToString() + search_date.Month.ToString("00") + "_F3_1_8_" + security_code + ".php?STK_NO=" + security_code + "&myear=" + search_date.Year.ToString() + "&mmon=" + search_date.Month.ToString("00");
            string select_html_string = "/html[1]/body[1]/table[1]/tr[3]/td[1]/table[3]";
            string web_result = network_setting.connect_by_httpwebrequest_get(url, "big5");

            if ("404" == web_result)
            {
                Debug.error(this, debug_level, "找不到網頁");
                return;
            }

            HtmlDocument web_context = new HtmlDocument();
            web_context.LoadHtml(web_result);
            HtmlNodeCollection child_nodes = web_context.DocumentNode.SelectSingleNode(select_html_string).ChildNodes;

            foreach (HtmlAgilityPack.HtmlNode node in child_nodes)
            {
                if (node.Name == "tr")
                {
                    num++;//該月份的天數
                    if (num > 2)//第一個是大標題  第二個是小標題 個股資訊由第三個<tr>開始
                    {
                        daily_trading_value_content content = new daily_trading_value_content();

                        string date = web_context.DocumentNode.SelectSingleNode(select_html_string + "/tr[" + num + "]/td[1]").InnerText;
                        string[] dates = date.Split('/');
                        content.date = (int.Parse(dates[0]) + 1911).ToString() + "/" + dates[1] + "/" + dates[2];
                        content.trade_volume = web_context.DocumentNode.SelectSingleNode(select_html_string + "/tr[" + num + "]/td[2]").InnerText;
                        content.trade_value = web_context.DocumentNode.SelectSingleNode(select_html_string + "/tr[" + num + "]/td[3]").InnerText;
                        content.opening_price = web_context.DocumentNode.SelectSingleNode(select_html_string + "/tr[" + num + "]/td[4]").InnerText;
                        content.highest_price = web_context.DocumentNode.SelectSingleNode(select_html_string + "/tr[" + num + "]/td[5]").InnerText;
                        content.lowest_price = web_context.DocumentNode.SelectSingleNode(select_html_string + "/tr[" + num + "]/td[6]").InnerText;
                        content.closing_price = web_context.DocumentNode.SelectSingleNode(select_html_string + "/tr[" + num + "]/td[7]").InnerText;
                        content.change = web_context.DocumentNode.SelectSingleNode(select_html_string + "/tr[" + num + "]/td[8]").InnerText;
                        content.transaction = web_context.DocumentNode.SelectSingleNode(select_html_string + "/tr[" + num + "]/td[9]").InnerText;
                        content.security_code = security_code.ToString();

                        date = date.Replace('/', '_');
                        date = date.Replace("_", "");

                        string key = security_code + (int.Parse(dates[0]) + 1911).ToString() + dates[1].ToString() + dates[2].ToString();//ex: 173020141001
                        stock_database.Add(key, content);
                    }
                }
            }
           
        }//end of function
        public void grep_OTC_daily_trading_data(DateTime search_date)
        {
            //http://www.otc.org.tw/web/stock/aftertrading/daily_trading_info/st43_print.php?l=zh-tw&d=102/4&stkno=6104&s=0,asc,0
            //個股日成交資訊 股票代號:6104 股票名稱:創惟 資料年月:102/4

            int num = 0;
            string url = "http://www.otc.org.tw/web/stock/aftertrading/daily_trading_info/st43_print.php?l=zh-tw&d=" + (search_date.Year - 1911) + "/" + search_date.Month + "&stkno=" + security_code + "&s=0,asc,0";
            string select_html_string = "/html[1]/body[1]/table[1]/tbody[1]";
            string web_result = network_setting.connect_by_httpwebrequest_get(url, "utf-8");
 
            HtmlDocument web_context = new HtmlDocument();
            web_context.LoadHtml(web_result);
            HtmlNodeCollection child_nodes = web_context.DocumentNode.SelectSingleNode(select_html_string).ChildNodes;

            foreach (HtmlAgilityPack.HtmlNode node in child_nodes)
            {
                if (node.Name == "tr")
                {
                    num++;

                    daily_trading_value_content content = new daily_trading_value_content();
                    string date = web_context.DocumentNode.SelectSingleNode(select_html_string + "/tr[" + num + "]/td[1]").InnerText;
                    string[] dates = date.Split('/');
                    if (dates[2].Length > 2)
                    {
                        Debug.error(this, debug_level, "刪除股票上櫃掛牌首日的星號");
                        dates[2] = dates[2].Remove(2);//刪除股票上櫃掛牌首日的星號
                    }

                    string trade_volume = web_context.DocumentNode.SelectSingleNode(select_html_string + "/tr[" + num + "]/td[2]").InnerText;//單位是千股
                    string trade_value = web_context.DocumentNode.SelectSingleNode(select_html_string + "/tr[" + num + "]/td[3]").InnerText;//單位是千元
                    trade_volume = trade_volume + ",000";//單位改成股
                    trade_value = trade_value + ",000";//單位改成元
                    content.date = (int.Parse(dates[0]) + 1911).ToString() + "/" + dates[1] + "/" + dates[2];
                    content.trade_volume = trade_volume;
                    content.trade_value = trade_value;
                    content.opening_price = web_context.DocumentNode.SelectSingleNode(select_html_string + "/tr[" + num + "]/td[4]").InnerText;
                    content.highest_price = web_context.DocumentNode.SelectSingleNode(select_html_string + "/tr[" + num + "]/td[5]").InnerText;
                    content.lowest_price = web_context.DocumentNode.SelectSingleNode(select_html_string + "/tr[" + num + "]/td[6]").InnerText;
                    content.closing_price = web_context.DocumentNode.SelectSingleNode(select_html_string + "/tr[" + num + "]/td[7]").InnerText;
                    content.change = web_context.DocumentNode.SelectSingleNode(select_html_string + "/tr[" + num + "]/td[8]").InnerText;
                    content.transaction = web_context.DocumentNode.SelectSingleNode(select_html_string + "/tr[" + num + "]/td[9]").InnerText;
                    content.security_code = security_code.ToString();

                    date = date.Replace('/', '_');
                    date = date.Replace("_", "");

                    string key = security_code + (int.Parse(dates[0]) + 1911).ToString() + dates[1].ToString() + dates[2].ToString();//ex: 173020141001
                    stock_database.Add(key, content);
                }
            }
            
        }

        //若是第一次產生的個股資料就跑這個function
        public void creat_stock_date()
        {
            Debug.error(this, debug_level, "開始更新個股" + security_code + " 日交易資料更新範圍" + start_date + " 至 " + end_date);

            for (DateTime date = start_date; date.Year < end_date.Year || date.Month <= end_date.Month; date = date.AddMonths(1))
            {
                //Debug.error(this, debug_level, date.ToString());
                if (company_type == "OTC")
                    grep_OTC_daily_trading_data(date);
                else
                    grep_daily_trading_data(date);
            }

            //建立新的xml檔案
            XmlTextWriter xml_file = new XmlTextWriter(file_path, Encoding.UTF8);//<?xml version="1.0" encoding="UTF-8"?>
            xml_file.Formatting = Formatting.Indented;//使用縮排
            xml_file.WriteStartDocument();

            //start <information>
            xml_file.WriteStartElement("information");
            xml_file.WriteAttributeString("title", "個股日成交資訊");
            if (company_type == "OTC")
                xml_file.WriteAttributeString("company_type", "上櫃");
            else
                xml_file.WriteAttributeString("company_type", "上市");
            xml_file.WriteAttributeString("stock_count", stock_database.Count().ToString());
            xml_file.WriteAttributeString("last_update_time", DateTime.Now.ToString());

            //start <attribute>
            xml_file.WriteStartElement("attribute");
            xml_file.WriteAttributeString("date", "日期");
            xml_file.WriteAttributeString("trade_volume", "成交股數");
            xml_file.WriteAttributeString("trade_value", "成交金額");
            xml_file.WriteAttributeString("opening_price", "開盤價");
            xml_file.WriteAttributeString("highest_price", "最高價");
            xml_file.WriteAttributeString("lowest_price", "最低價");
            xml_file.WriteAttributeString("closing_price", "收盤價");
            xml_file.WriteAttributeString("change", "漲跌價差");
            xml_file.WriteAttributeString("transaction", "成交筆數");

            //加入個股資訊 
            for (int i = 0; i < stock_database.Count(); i++)
            {
                //Console.WriteLine(debug() + "寫入第" + (i + 1) + "筆資料，共" + stock_database.Count() + "筆");

                xml_file.WriteStartElement("stock");//<stock>
                xml_file.WriteAttributeString("date", stock_database.ElementAt(i).Value.date);
                xml_file.WriteAttributeString("trade_volume", stock_database.ElementAt(i).Value.trade_volume);
                xml_file.WriteAttributeString("trade_value", stock_database.ElementAt(i).Value.trade_value);
                xml_file.WriteAttributeString("opening_price", stock_database.ElementAt(i).Value.opening_price);
                xml_file.WriteAttributeString("highest_price", stock_database.ElementAt(i).Value.highest_price);
                xml_file.WriteAttributeString("lowest_price", stock_database.ElementAt(i).Value.lowest_price);
                xml_file.WriteAttributeString("closing_price", stock_database.ElementAt(i).Value.closing_price);
                xml_file.WriteAttributeString("change", stock_database.ElementAt(i).Value.change);
                xml_file.WriteAttributeString("transaction", stock_database.ElementAt(i).Value.transaction);

                string date = stock_database.ElementAt(i).Value.date;
                date = date.Replace('/', '_');
                date = date.Replace("_", "");
                xml_file.WriteAttributeString("id", security_code.ToString() + date);

                xml_file.WriteEndElement();//</stock>  

            }
            xml_file.WriteEndElement();
            //</attribute>

            xml_file.WriteEndElement();
            //end <information>
            //XML結尾
            xml_file.WriteEndDocument();
            xml_file.Close();

            stock_database.Clear();//清空dictionary內容
            Debug.error(this, debug_level, "完成更新個股" + security_code + "的日交易資料");
        }//end of function

        public void update_stock_date()
        {
            int is_search_date_too_late = end_date.CompareTo(start_date);
            //xml內年份大於要搜尋的年份 ex:search 2000/3 exist 2001/3  is_search_date_too_late=-1  
            if (-1 == is_search_date_too_late)
            {
                Debug.error(this, debug_level, "搜尋資料已在範圍內" + start_date + "年 =" + end_date + "年");
                return;
            }
            //xml內年份等於要搜尋的年份 ex:search 2000/3 exist 2000/4  is_search_date_too_late=0
            if (0 == is_search_date_too_late)
            {
                Debug.error(this, debug_level, "搜尋資料已在範圍內" + start_date + "年 >" + end_date + "年");
                return;
            }

            //資料更新範圍:xml裡的最新資料到目前的最新資料
            for (DateTime date = start_date; date.Year < end_date.Year || date.Month <= end_date.Month; date = date.AddMonths(1))
            {
                //Debug.error(this, debug_level, date.ToString());
                if (company_type == "OTC")
                    grep_OTC_daily_trading_data(date);
                else
                    grep_daily_trading_data(date);
            }

            //儲存到xml檔案
            int new_add_count = 0;
            XmlDocument doc = new XmlDocument();
            doc.Load(file_path);
            for (int i = 0; i < stock_database.Count(); i++)
            {
                XmlNode node_exist = doc.SelectSingleNode("information/attribute/stock[@id=" + stock_database.ElementAt(i).Key + "]");

                //判斷資料庫裡是否有重複的檔案
                if (node_exist != null)
                {
                    //Debug.error(this, debug_level, "已存在資料 id:" + stock_database.ElementAt(i).Key);
                }
                else
                {
                    new_add_count++;
                    XmlNode parent_node = doc.SelectSingleNode("information/attribute");
                    XmlElement new_node = doc.CreateElement("stock"); //add new node

                    new_node.SetAttribute("date", stock_database.ElementAt(i).Value.date);
                    new_node.SetAttribute("trade_volume", stock_database.ElementAt(i).Value.trade_volume);
                    new_node.SetAttribute("trade_value", stock_database.ElementAt(i).Value.trade_value);
                    new_node.SetAttribute("opening_price", stock_database.ElementAt(i).Value.opening_price);
                    new_node.SetAttribute("highest_price", stock_database.ElementAt(i).Value.highest_price);
                    new_node.SetAttribute("lowest_price", stock_database.ElementAt(i).Value.lowest_price);
                    new_node.SetAttribute("closing_price", stock_database.ElementAt(i).Value.closing_price);
                    new_node.SetAttribute("change", stock_database.ElementAt(i).Value.change);
                    new_node.SetAttribute("transaction", stock_database.ElementAt(i).Value.transaction);
                    string date = stock_database.ElementAt(i).Value.date;
                    date = date.Replace('/', '_');
                    date = date.Replace("_", "");
                    new_node.SetAttribute("id", security_code.ToString() + date);
                    parent_node.InsertAfter(new_node, parent_node.LastChild);
                }
            }

            //有些公司已下市  不會更新到
            if (stock_database.Count != 0)
            {
                //更新<information>資訊
                XmlElement information_element = (XmlElement)doc.SelectSingleNode("information");
                //stock_count
                int stock_count = int.Parse(information_element.GetAttribute("stock_count"));
                stock_count += new_add_count;
                information_element.SetAttribute("stock_count", stock_count.ToString());
                //last_update_time
                information_element.SetAttribute("last_update_time", DateTime.Now.ToString());
                //latest_stock_date
                information_element.SetAttribute("latest_stock_date", stock_database.ElementAt(stock_database.Count - 1).Value.date);

                doc.Save(file_path);
                stock_database.Clear();//清除掉已經存到資料庫的資料
                Debug.error(this, debug_level, "完成更新個股" + security_code + "的日交易資料");
            }
            else
            {
                Debug.error(this, debug_level, "error 查無資料個股: " + security_code);
            }
        }//end of update_stock_information()

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.D0 || e.Key == Key.D1 || e.Key == Key.D2 || e.Key == Key.D3 || e.Key == Key.D4
                || e.Key == Key.D5 || e.Key == Key.D6 || e.Key == Key.D7 || e.Key == Key.D8 || e.Key == Key.D9)
            {
                //只能輸入0到9
            }
            else
            {
                e.Handled = true;
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            download_single_stock_data();
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            initial();
        }
    }
}
