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

namespace moneyMaker
{
    /// <summary>
    /// screen_reading_the_tape.xaml 的互動邏輯
    /// 看盤畫面
    /// 內含版面:K線圖版面,融資融券餘額版面,三大法人版面,月營收版面,股權分散版面,個股事件版面,成交量版面,平均每筆交易金額版面,外資及陸資投資持股統計
    /// </summary>
    public partial class screen_reading_the_tape : UserControl
    {
        char debug_level = '1';
        public DateTime start_date;
        public DateTime end_date = DateTime.Today;

        public screen_reading_the_tape()
        {
            InitializeComponent();
            //initial_time_setting(new DateTime(1988, 3, 18));
            config.screen_reading_the_tape = this;
        }

        //設定搜尋範圍時間的值
        public void initial_time_setting(DateTime _start_date)
        {
            comboBox_year.Items.Clear();
            comboBox_month.Items.Clear();
            comboBox_day.Items.Clear();

            //年
            for (int i = _start_date.Year; i <= DateTime.Today.Year; i++)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = i;
                comboBox_year.Items.Add(item);
            }
            //月
            for (int i = 1; i <= 12; i++)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = i;
                comboBox_month.Items.Add(item);
            }
            //日
            for (int i = 1; i <= 31; i++)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = i;
                comboBox_day.Items.Add(item);
            }

            //如果該股票上市日期小於要顯示的時間區間就由上市日期做起始日
            if (DateTime.Today.Year - _start_date.Year < config.time_range)
            {
                comboBox_year.SelectedIndex = 0;
                comboBox_month.SelectedIndex = _start_date.Month - 1;
                comboBox_day.SelectedIndex = _start_date.Day - 1;
                string date = comboBox_year.Text + "/" + comboBox_month.Text + "/" + comboBox_day.Text;
                start_date = DateTime.Parse(date);
            }
            else
            {
                comboBox_year.SelectedIndex = comboBox_year.Items.Count - config.time_range;
                comboBox_month.SelectedIndex = 0;
                comboBox_day.SelectedIndex = 0;
                string date = comboBox_year.Text + "/" + comboBox_month.Text + "/" + comboBox_day.Text;
                start_date = DateTime.Parse(date);
            }
        }

        //載入個股資料按鈕
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if (textBox1.Text.Length == 4)
            {
                string security_code = textBox1.Text;
                if (System.IO.File.Exists(Path.daily_trading_value_path + security_code + ".xml"))
                {
                    detect_file(security_code);
                    initial(security_code);
                }
                else
                {
                    MessageBox.Show("該股票資料不存在，請先下載");
                }
            }
            else
            {
                MessageBox.Show("股票代號格式錯誤");
            }
        }
        public void initial(string security_code)
        {
            //設定起始日期
            start_date = DateTime.Parse(comboBox_year.Text + "/" + comboBox_month.Text + "/" + comboBox_day.Text);

            Debug.error(this, debug_level, "initial");
            candlestick_chart.initial(security_code);
            trade_volume_chart.initial(security_code);
            transaction_chart.initial(security_code);
            stock_event_chart.initial(security_code);
            monthly_recurring_revenue_chart.initial(security_code);
            TDCC_stock_dispersion_chart.initial(security_code);
            institutional_investors_chart.initial(security_code);
            deal_on_credit_chart.initial(security_code);

            foreign_investors_percentage_chart.initial(security_code);

            Debug.error(this, debug_level, "initial X");
        }
        //檢測資料檔案是否齊全
        public void detect_file(string security_code)
        {
            //檢查個股事件檔案是否存在
            screen_stock_event_editor editor = new screen_stock_event_editor();
            editor.detect_file(security_code);
        }

        private void textBox1_TextChanged(object sender, TextChangedEventArgs e)
        {

            //在輸入第四個代碼時查詢該個股資料
            if (textBox1.Text.Length == 4)
            {
                string key = textBox1.Text;
                if (stock_dictionary.security_code_information.ContainsKey(key) == true)
                {
                    string stock_name = stock_dictionary.security_code_information[key].stock_name;
                    string market_type = stock_dictionary.security_code_information[key].market_type;
                    string industry_type = stock_dictionary.security_code_information[key].industry_type;
                    string public_date = stock_dictionary.security_code_information[key].public_date;

                    initial_time_setting(DateTime.Parse(public_date));//重新設定起始時間

                    label1.Content = "股票名稱: " + stock_name + "(" + market_type + ")";
                    label2.Content = "產業別: " + industry_type + " 上市櫃日期: " + public_date;
                }
                else
                {
                    label1.Content = "無此股票代號";
                }
            }
            else
            {
                label1.Content = "";
                label2.Content = "";
            }
        }

        //拉霸統一控制
        public void change_all_scrollviewer(double offset)
        {
            candlestick_chart.surfaceScrollViewer1.ScrollToHorizontalOffset(offset);//K線圖版面
            trade_volume_chart.surfaceScrollViewer1.ScrollToHorizontalOffset(offset);//成交量版面
            transaction_chart.surfaceScrollViewer1.ScrollToHorizontalOffset(offset);//成交筆數版面
            stock_event_chart.surfaceScrollViewer1.ScrollToHorizontalOffset(offset);//時間軸版面
            monthly_recurring_revenue_chart.surfaceScrollViewer1.ScrollToHorizontalOffset(offset);//月營收版面
            TDCC_stock_dispersion_chart.surfaceScrollViewer1.ScrollToHorizontalOffset(offset);//集保戶股權分散表版面
            institutional_investors_chart.surfaceScrollViewer1.ScrollToHorizontalOffset(offset);//三大法人版面
            deal_on_credit_chart.surfaceScrollViewer1.ScrollToHorizontalOffset(offset);//融資融券餘額版面
            foreign_investors_percentage_chart.surfaceScrollViewer1.ScrollToHorizontalOffset(offset);//外資及陸資投資持股統計版面
        }


        //統一顯示日期特徵反白 使用datetime
        public void show_date_characteristic(DateTime date)
        {
            //日線圖
            for (int i = 0; i < candlestick_chart.stackPanel1.Children.Count; i++)
            {
                chart.candlestick_content child = (chart.candlestick_content)candlestick_chart.stackPanel1.Children[i];
                if (date == child.date)
                {
                    child.characteristic_grid.Visibility = System.Windows.Visibility.Visible;
                }
            }

            //三大法人
            for (int i = 0; i < institutional_investors_chart.stackPanel1.Children.Count; i++)
            {
                chart.institutional_investors_content child = (chart.institutional_investors_content)institutional_investors_chart.stackPanel1.Children[i];
                if (date == child.date)
                {
                    child.characteristic_grid.Visibility = System.Windows.Visibility.Visible;
                }
            }
            
            //成交量圖
            for (int i = 0; i < trade_volume_chart.volume_stackpanel.Children.Count; i++)
            {
                chart.volume child = (chart.volume)trade_volume_chart.volume_stackpanel.Children[i];
                if (date == child.date)
                {
                    child.characteristic_grid.Visibility = System.Windows.Visibility.Visible;
                }
            }

            //平均每筆交易金額圖
            for (int i = 0; i < transaction_chart.stackPanel1.Children.Count; i++)
            {
                chart.volume child = (chart.volume)transaction_chart.stackPanel1.Children[i];
                if (date == child.date)
                {
                    child.characteristic_grid.Visibility = System.Windows.Visibility.Visible;
                }
            }

            //融資融券餘額
            for (int i = 0; i < deal_on_credit_chart.stackPanel1.Children.Count; i++)
            {
                chart.deal_on_credit_content child = (chart.deal_on_credit_content)deal_on_credit_chart.stackPanel1.Children[i];
                if (date == DateTime.Parse(child.stock_data.date))
                {
                    child.characteristic_grid.Visibility = System.Windows.Visibility.Visible;
                }
            }

            //陸資及外資持股比例
            for (int i = 0; i < foreign_investors_percentage_chart.stackPanel1.Children.Count; i++)
            {
                chart.volume child = (chart.volume)foreign_investors_percentage_chart.stackPanel1.Children[i];
                if (date == child.date)
                {
                    child.characteristic_grid.Visibility = System.Windows.Visibility.Visible;
                }
            }
        }

        //統一隱藏日期特徵反白 使用datetime
        public void hide_date_characteristic(DateTime date)
        {
            //日線圖
            for (int i = 0; i < candlestick_chart.stackPanel1.Children.Count; i++)
            {
                chart.candlestick_content child = (chart.candlestick_content)candlestick_chart.stackPanel1.Children[i];
                if (date == child.date)
                {
                    child.characteristic_grid.Visibility = System.Windows.Visibility.Hidden;
                }
            }

            //三大法人
            for (int i = 0; i < institutional_investors_chart.stackPanel1.Children.Count; i++)
            {
                chart.institutional_investors_content child = (chart.institutional_investors_content)institutional_investors_chart.stackPanel1.Children[i];
                if (date == child.date)
                {
                    child.characteristic_grid.Visibility = System.Windows.Visibility.Hidden;
                }
            }

            //成交量圖
            for (int i = 0; i < trade_volume_chart.volume_stackpanel.Children.Count; i++)
            {
                chart.volume child = (chart.volume)trade_volume_chart.volume_stackpanel.Children[i];
                if (date == child.date)
                {
                    child.characteristic_grid.Visibility = System.Windows.Visibility.Hidden;
                }
            }

            //平均每筆交易金額圖
            for (int i = 0; i < transaction_chart.stackPanel1.Children.Count; i++)
            {
                chart.volume child = (chart.volume)transaction_chart.stackPanel1.Children[i];
                if (date == child.date)
                {
                    child.characteristic_grid.Visibility = System.Windows.Visibility.Hidden;
                }
            }

            //融資融券餘額
            for (int i = 0; i < deal_on_credit_chart.stackPanel1.Children.Count; i++)
            {
                chart.deal_on_credit_content child = (chart.deal_on_credit_content)deal_on_credit_chart.stackPanel1.Children[i];
                if (date == DateTime.Parse(child.stock_data.date))
                {
                    child.characteristic_grid.Visibility = System.Windows.Visibility.Hidden;
                }
            }

            //陸資及外資持股比例
            for (int i = 0; i < foreign_investors_percentage_chart.stackPanel1.Children.Count; i++)
            {
                chart.volume child = (chart.volume)foreign_investors_percentage_chart.stackPanel1.Children[i];
                if (date == child.date)
                {
                    child.characteristic_grid.Visibility = System.Windows.Visibility.Hidden;
                }
            }
        }

        //統一使用日線圖資料
        private void show_daily_candlestick_button_Click(object sender, RoutedEventArgs e)
        {
            //candlestick_chart.process_data((int)config.time_period.daily);
            //trade_volume_chart.process_data((int)config.time_period.daily);
            //transaction_chart.process_data((int)config.time_period.daily);
        }

        //統一使用周線圖資料
        private void show_weekly_candlestick_button_Click(object sender, RoutedEventArgs e)
        {
            //candlestick_chart.process_data((int)config.time_period.weekly);
            //trade_volume_chart.process_data((int)config.time_period.weekly);
            //transaction_chart.process_data((int)config.time_period.weekly);
        }

        //統一使用月線圖資料
        private void show_monthly_candlestick_button_Click(object sender, RoutedEventArgs e)
        {
            //candlestick_chart.process_data((int)config.time_period.monthly);
            //trade_volume_chart.process_data((int)config.time_period.monthly);
            //transaction_chart.process_data((int)config.time_period.monthly);
        }
    }
}
