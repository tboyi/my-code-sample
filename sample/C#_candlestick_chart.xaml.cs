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
using System.Xml;
using System.Xml.Linq;

namespace moneyMaker.chart
{
    /// <summary>
    /// candlestick_chart.xaml 的互動邏輯
    /// 日交易版面
    /// </summary>
    public partial class candlestick_chart : UserControl
    {
        char debug_level = '1';
        string security_code;
        public config.time_period period = config.time_period.daily;
        public decimal max_price = 0;
        public decimal min_price = 1000;
        DateTime start_date;
        DateTime end_date;

        public candlestick_chart()
        {
            InitializeComponent();
        }

        public void initial(string _security_code)
        {
            Debug.error(this, debug_level, "initial");

            security_code = _security_code;
            start_date = config.screen_reading_the_tape.start_date;
            end_date = config.screen_reading_the_tape.end_date;

            load_xml_data();
            process_data(config.time_period.daily);

            Debug.error(this, debug_level, "initial X");
        }

        //從xml讀取資料到Dictionary，並取得最大成交量和交易金額最大和最小值 個股每月工作天數
        public void load_xml_data()
        {
            Debug.error(this, debug_level, "load_xml_data");
            stock_dictionary.stock_daily_trading_value.Clear();
            stock_dictionary.stock_working_day_information.Clear();

            string file_path = Path.daily_trading_value_path + security_code + ".xml";

            XDocument xml = XDocument.Load(file_path);
            var linq_stock = from linq in xml.Descendants()
                             where linq.Name.LocalName == "stock"
                             select new
                             {
                                 date_linq = linq.Attribute("date"),
                                 trade_volume_linq = linq.Attribute("trade_volume"),
                                 trade_value_linq = linq.Attribute("trade_value"),
                                 opening_price_linq = linq.Attribute("opening_price"),
                                 highest_price_linq = linq.Attribute("highest_price"),
                                 lowest_price_linq = linq.Attribute("lowest_price"),
                                 closing_price_linq = linq.Attribute("closing_price"),
                                 change_linq = linq.Attribute("change"),
                                 transaction_linq = linq.Attribute("transaction"),
                                 id_linq = linq.Attribute("id"),
                             };

            foreach (var result in linq_stock)
            {
                string opening_price = result.opening_price_linq.Value;
                string lowest_price = result.lowest_price_linq.Value;
                string highest_price = result.highest_price_linq.Value;
                string closing_price = result.closing_price_linq.Value;
                if ("--" == opening_price)
                {
                    opening_price = "0";
                }

                if ("--" == lowest_price)
                {
                    lowest_price = "0";
                }

                if ("--" == highest_price)
                {
                    highest_price = "0";
                }

                if ("--" == closing_price)
                {
                    closing_price = "0";
                }

                daily_trading_value_content content = new daily_trading_value_content();
                content.date = result.date_linq.Value;
                content.trade_volume = result.trade_volume_linq.Value;
                content.trade_value = result.trade_value_linq.Value;
                content.opening_price = opening_price;
                content.highest_price = highest_price;
                content.lowest_price = lowest_price;
                content.closing_price = closing_price;
                content.change = result.change_linq.Value;
                content.transaction = result.transaction_linq.Value;

                stock_dictionary.stock_daily_trading_value.Add(result.id_linq.Value, content);

                //如果交易日內容有問題則不載入這天的交易內容
                //為何還要加入這天有問題的日子?因為時間軸是只要在daily_trading_value/xxxx.xml裡面有出現的日子都會加入
                //為了保持時間軸的寬度正確仍然加入這天
                if ("0" == result.transaction_linq.Value)//交易次數是0
                {
                    continue;
                }
            }

            //找出交易金額的最大值和最小值
            for (DateTime date = start_date; date <= end_date; date = date.AddDays(1))
            {
                string key = security_code.ToString() + date.Year.ToString() + date.Month.ToString("00") + date.Day.ToString("00");
                if (stock_dictionary.stock_daily_trading_value.ContainsKey(key))
                {
                    decimal lowest_price = decimal.Parse(stock_dictionary.stock_daily_trading_value[key].lowest_price);
                    decimal highest_price = decimal.Parse(stock_dictionary.stock_daily_trading_value[key].highest_price);

                    if (lowest_price < min_price)
                    {
                        min_price = lowest_price - lowest_price * 1 / 10;
                    }
                    if (highest_price > max_price)
                    {
                        max_price = highest_price + highest_price * 2 / 10;
                    }
                }
            }

            //計算每月工作天
            int working_day = 0;
            int non_working_day = 0;
            for (DateTime date = start_date; date <= end_date; date = date.AddDays(1))
            {
                string stock_key = security_code.ToString() + date.Year.ToString() + date.Month.ToString("00") + date.Day.ToString("00");
                if (stock_dictionary.stock_daily_trading_value.ContainsKey(stock_key))
                {
                    working_day++;
                }
                else
                {
                    non_working_day++;
                }

                //該月最後一天或迴圈最後一天就結算
                if (date.Month != date.AddDays(1).Month || date == DateTime.Today)
                {
                    for (DateTime create_date = DateTime.Parse(date.Year.ToString() + "/" + date.Month.ToString() + "/1"); create_date <= date; create_date = create_date.AddDays(1))
                    {
                        string stock_working_day_key = create_date.Year.ToString() + create_date.Month.ToString("00") + create_date.Day.ToString("00") + security_code.ToString();
                        working_date date_content = new working_date();
                        date_content.working_days = working_day;
                        date_content.non_working_days = non_working_day;
                        stock_dictionary.stock_working_day_information.Add(stock_working_day_key, date_content);
                    }

                    working_day = 0;
                    non_working_day = 0;
                }
            }

            Debug.error(this, debug_level, "load_xml_data X");
        }
        
        public void process_data(config.time_period select_period)
        {
            Debug.error(this, debug_level, "process_data");

            stackPanel1.Children.Clear();//清掉stack上的值

            period = select_period;
            switch (select_period)
            {
                case config.time_period.daily:
                    add_daily_candlestick_chart();
                    break;
                case config.time_period.weekly:
                    add_weekly_candlestick_chart();
                    break;
                case config.time_period.monthly:
                    add_monthly_candlestick_chart();
                    break;
            }

            setting_price_line();

            Debug.error(this, debug_level, "process_data X");
        }
        //日K線圖
        public void add_daily_candlestick_chart()
        {
            Debug.error(this, debug_level, "add_daily_candlestick_chart");

            for (DateTime date = start_date; date.Date <= end_date; date = date.AddDays(1))
            {
                string TWSE_key = date.Year.ToString() + date.Month.ToString("00") + date.Day.ToString("00");
                string stock_key = security_code.ToString() + TWSE_key;
                candlestick_content child = new candlestick_content();
                if (stock_dictionary.TWSE_daily_trading_value.ContainsKey(TWSE_key)
                    && stock_dictionary.stock_daily_trading_value.ContainsKey(stock_key))//開盤日 and 該個股有資料
                {

                    //開盤 最高 最低 收盤 chart最上面的值 chart最底部的值 
                    child.set_value(stock_key,
                                     decimal.Parse(stock_dictionary.stock_daily_trading_value[stock_key].opening_price),
                                     decimal.Parse(stock_dictionary.stock_daily_trading_value[stock_key].highest_price),
                                     decimal.Parse(stock_dictionary.stock_daily_trading_value[stock_key].lowest_price),
                                     decimal.Parse(stock_dictionary.stock_daily_trading_value[stock_key].closing_price),
                                     max_price, min_price);
                    child.trade_value = decimal.Parse(stock_dictionary.stock_daily_trading_value[stock_key].trade_value);
                    child.Width = config.width_of_value_in_chart;

                    //判斷是否加入 除權息 增資 減資 事件
                    string public_subscription_key = date.Year.ToString() + date.Month.ToString("00") + date.Day.ToString("00") + security_code;
                    string capital_reduction_key = date.Year.ToString() + date.Month.ToString("00") + date.Day.ToString("00") + security_code;
                    string DR_announcement_key = security_code + date.Year.ToString() + date.Month.ToString("00") + date.Day.ToString("00");

                    //除權息
                    if (stock_dictionary.DR_announcement_dictionary.ContainsKey(DR_announcement_key))
                    {
                        if (stock_dictionary.DR_announcement_dictionary[DR_announcement_key].XR_or_XD.Contains("息"))
                        {
                            Image img = new Image();
                            img.Source = new BitmapImage(new Uri(Path.system_image_directory_path + "DR_announcement_XD.png", UriKind.Absolute));
                            child.stackpanel_stock_event.Children.Add(img);
                        }

                        if (stock_dictionary.DR_announcement_dictionary[DR_announcement_key].XR_or_XD.Contains("權"))
                        {
                            Image img = new Image();
                            img.Source = new BitmapImage(new Uri(Path.system_image_directory_path + "DR_announcement_XR.png", UriKind.Absolute));
                            child.stackpanel_stock_event.Children.Add(img);
                        }
                    }

                    //增資
                    if (stock_dictionary.public_subscription_dictionary.ContainsKey(public_subscription_key))
                    {
                        Image img = new Image();
                        img.Source = new BitmapImage(new Uri(Path.system_image_directory_path + "pic_public_subscription.png", UriKind.Absolute));
                        child.stackpanel_stock_event.Children.Add(img);
                    }

                    //減資
                    if (stock_dictionary.capital_reduction_dictionary.ContainsKey(capital_reduction_key))
                    {
                        Image img = new Image();
                        img.Source = new BitmapImage(new Uri(Path.system_image_directory_path + "pic_capital_reduction.png", UriKind.Absolute));
                        child.stackpanel_stock_event.Children.Add(img);
                    }
                }
                else//非營業日  
                {
                    child.set_value(stock_key, 0, 0, 0, 0, max_price, min_price);
                    child.Width = config.width_of_empty_date_in_chart;
                }
                
                child.date = date;
                stackPanel1.Children.Insert(0, child);

            }//end of loop

            Debug.error(this, debug_level, "add_daily_candlestick_chart X");
        }

        //周K線圖
        public void add_weekly_candlestick_chart()
        {
            Debug.error(this, debug_level, "add_weekly_candlestick_chart");

            decimal highest_price = 0;
            decimal lowest_price = 9999;
            decimal opening_price = 0;
            decimal closing_price = 0;
            decimal trade_value = 0;

            int working_day = 0;
            int non_working_day = 0;

            for (DateTime date = start_date; date.Date <= end_date; date = date.AddDays(1))
            {
                string key = date.Year.ToString() + date.Month.ToString("00") + date.Day.ToString("00");
                string stock_key = security_code.ToString() + key;

                if (stock_dictionary.TWSE_daily_trading_value.ContainsKey(key))//開盤日 
                {
                    if (stock_dictionary.stock_daily_trading_value.ContainsKey(stock_key))//該個股有資料
                    {
                        //取最高價
                        if (highest_price < decimal.Parse(stock_dictionary.stock_daily_trading_value[stock_key].highest_price))
                        {
                            highest_price = decimal.Parse(stock_dictionary.stock_daily_trading_value[stock_key].highest_price);
                        }

                        //取最低價
                        if (lowest_price > decimal.Parse(stock_dictionary.stock_daily_trading_value[stock_key].lowest_price))
                        {
                            lowest_price = decimal.Parse(stock_dictionary.stock_daily_trading_value[stock_key].lowest_price);
                        }

                        //取開盤價
                        if (opening_price == 0)//取第一個值
                        {
                            opening_price = decimal.Parse(stock_dictionary.stock_daily_trading_value[stock_key].opening_price);
                        }

                        //取收盤價
                        closing_price = decimal.Parse(stock_dictionary.stock_daily_trading_value[stock_key].closing_price);//取最後一個值

                        //成交價
                        trade_value = trade_value + decimal.Parse(stock_dictionary.stock_daily_trading_value[stock_key].trade_value);

                    }
                    working_day++;
                }
                else//非營業日
                {
                    non_working_day++;
                }

                //星期日或最後一天就結算這一周的值
                if (date.DayOfWeek == DayOfWeek.Sunday || date == DateTime.Today)
                {
                    candlestick_content child = new candlestick_content();
                    if (opening_price != 0)
                    {
                        //開盤 最高 最低 收盤 chart最上面的值 chart最底部的值 
                        child.set_value(stock_key, opening_price, highest_price, lowest_price, closing_price, max_price, min_price);
                    }
                    else//沒取到開盤價代表載入日期是從假日載入的
                    {
                        child.set_value(stock_key, 0, 0, 0, 0, max_price, min_price);
                    }
                    child.trade_value = trade_value;
                    child.date = date;
                    stackPanel1.Children.Insert(0, child);
                    child.Width = config.width_of_value_in_chart * working_day + config.width_of_empty_date_in_chart * non_working_day;

                    opening_price = 0;//要歸零給下一周去判斷是否要取值
                    lowest_price = 9999;
                    highest_price = 0;
                    trade_value = 0;
                    working_day = 0;
                    non_working_day = 0;
                }
            }//end of loop

            Debug.error(this, debug_level, "add_weekly_candlestick_chart X");
        }

        //月K線圖
        public void add_monthly_candlestick_chart()
        {
            Debug.error(this, debug_level, "add_monthly_candlestick_chart");

            decimal highest_price = 0;
            decimal lowest_price = 9999;
            decimal opening_price = 0;
            decimal closing_price = 0;
            decimal trade_value = 0;

            for (DateTime date = start_date; date.Date <= end_date; date = date.AddDays(1))
            {
                string key = date.Year.ToString() + date.Month.ToString("00") + date.Day.ToString("00");
                string TWSE_working_date_key = date.Year.ToString() + "/" + date.Month.ToString("00");
                string stock_key = security_code.ToString() + key;

                if (stock_dictionary.TWSE_daily_trading_value.ContainsKey(key))//開盤日 
                {
                    if (stock_dictionary.stock_daily_trading_value.ContainsKey(stock_key))//該個股有資料
                    {
                        //取最高價
                        if (highest_price < decimal.Parse(stock_dictionary.stock_daily_trading_value[stock_key].highest_price))
                        {
                            highest_price = decimal.Parse(stock_dictionary.stock_daily_trading_value[stock_key].highest_price);
                        }

                        //取最低價
                        if (lowest_price > decimal.Parse(stock_dictionary.stock_daily_trading_value[stock_key].lowest_price))
                        {
                            lowest_price = decimal.Parse(stock_dictionary.stock_daily_trading_value[stock_key].lowest_price);
                        }

                        //取開盤價
                        if (opening_price == 0)//取第一個值
                        {
                            opening_price = decimal.Parse(stock_dictionary.stock_daily_trading_value[stock_key].opening_price);
                        }

                        //取收盤價
                        closing_price = decimal.Parse(stock_dictionary.stock_daily_trading_value[stock_key].closing_price);//取最後一個值

                        //成交價
                        trade_value = trade_value + decimal.Parse(stock_dictionary.stock_daily_trading_value[stock_key].trade_value);

                    }
                }

                //這個月的最後一天或最後一天結算
                if (date.Month != date.AddDays(1).Month || date == DateTime.Today)
                {
                    candlestick_content child = new candlestick_content();
                    int working_day = stock_dictionary.TWSE_working_date_information[TWSE_working_date_key].working_days;
                    int non_working_day = stock_dictionary.TWSE_working_date_information[TWSE_working_date_key].non_working_days;

                    if (opening_price != 0)
                    {
                        //開盤 最高 最低 收盤 chart最上面的值 chart最底部的值 
                        child.set_value(stock_key, opening_price, highest_price, lowest_price, closing_price, max_price, min_price);
                    }
                    else//沒取到開盤價 代表載入日期剛好碰到都是假日 
                    {
                        child.set_value(stock_key, 0, 0, 0, 0, max_price, min_price);
                    }
                    child.trade_value = trade_value;
                    child.date = date;
                    child.Width = config.width_of_value_in_chart * working_day + config.width_of_empty_date_in_chart * non_working_day;
                    stackPanel1.Children.Insert(0, child);

                    //歸零
                    opening_price = 0;//要歸零給下一個月去判斷是否要取值
                    lowest_price = 9999;
                    highest_price = 0;
                    trade_value = 0;
                }
            }//end of loop

            Debug.error(this, debug_level, "add_monthly_candlestick_chart X");
        }

        //指示價位的橫條線
        public void setting_price_line()
        {
            candlestick_space_grid.Children.Clear();

            //顯示價位的線
            for (int i = 1; i <= 5; i++)
            {
                double space = stackPanel1.ActualHeight / 6;

                Grid grid = new Grid();
                grid.Background = Brushes.Black;
                grid.Height = 0.7;
                grid.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
                grid.Margin = new Thickness(0, 0, 0, (space * i));

                Label label = new Label();
                label.Content = ((max_price - min_price) / 6) * i + min_price;
                label.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
                label.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                label.Margin = new Thickness(0, 0, 0, (space * i));
                label.Foreground = Brushes.Black;
                candlestick_space_grid.Children.Add(grid);
                candlestick_space_grid.Children.Add(label);
            }
        }

        private void scrollviewer1_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (surfaceScrollViewer1.HorizontalOffset > 0)//這邊要除錯，不然reading_the_tape預覽畫面會有問題
            {
                config.screen_reading_the_tape.change_all_scrollviewer(surfaceScrollViewer1.HorizontalOffset);
            }
        }

        private void show_daily_candlestick_button_Click(object sender, RoutedEventArgs e)
        {
            process_data(config.time_period.daily);
        }

        private void show_weekly_candlestick_button_Click(object sender, RoutedEventArgs e)
        {
            process_data(config.time_period.weekly);
        }

        private void show_monthly_candlestick_button_Click(object sender, RoutedEventArgs e)
        {
            process_data(config.time_period.monthly);
        }
    }
}
