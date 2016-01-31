<?php
	include("./get_xml_data.php");
	date_default_timezone_set("Asia/Taipei");//set up time zone or the default time will decrease 8 hour.
	$get_date = $_POST['datepicker'];
	$button_select_date = $_POST['button_select_date'];
	$submit_date = $_POST['submit_date'];
	$PPFD = 0.017 / 86400;// the lux to ppfd formula (Lux to PPF (μmol m-2 s-1))
	$array_attribute_data_count = 1440;
	$array_attribute_count = 4;//temperature, humidity, lux, total_lux_sum, id
	//$exist_file_id = array("WA_001", "WA002");
	$file_directory_path = "./database/";
	$date_exist = true;
	
	//echo "submit_date:".$submit_date." button_select_date:".$button_select_date." get_date:".$get_date;
	if(null == $get_date)
	{
	    $year = date("Y");
	    $month = date("m");
	    $day = date("d");
	}
	else
	{
	    $dates = explode("/", $get_date);
        $month = $dates[0]; 
        $day = $dates[1]; 
        $year = $dates[2]; 
	}
	
	if ($button_select_date == "今天") {
        $year = date("Y");
	    $month = date("m");
	    $day = date("d");
    }
    else if ($button_select_date == "前一天") {
        
        $time = strtotime($submit_date) - 60*60*24;
        $yesterday = date('Y/m/d',$time);
        
        $dates = explode("/", $yesterday);
	    $month = $dates[1]; 
        $day = $dates[2]; 
        $year = $dates[0]; 
        
    }
    else if ($button_select_date == "明天") {
        $time = strtotime($submit_date) + 60*60*24;
        $tomorrow = date('Y/m/d',$time);
        
        $dates = explode("/", $tomorrow);
	    $month = $dates[1]; 
        $day = $dates[2]; 
        $year = $dates[0]; 
    }
    
    //$day = 27;
    
	//scan how many file are there
    $files = scandir($file_directory_path);
	for($i = 0; $i < count($files) - 2; $i++)
	{
	    $file_path = $file_directory_path.$files[$i+2]."/".$year.$month."/".$year.$month.$day.".xml";
	    if(file_exists($file_path))
	    {
	        //echo "add file:".$file_path."<br/>";
	        $array_data_path[] = $file_path;
	    }
	    
	}
	
	//$array_data_path[0] = "./database/WA001/201511/".$year.$month.$day .".xml";
	//$array_data_path[1] = "./database/WA002/201511/".$year.$month.$day .".xml";
	
	$file_count = count($array_data_path);
	
	if ($file_count == 0) 
	{
	    $file_count = 0;
	    $date_exist = false;
        //echo "查無".$year."/".$month."/".$day."的資料!";
    } 
    
	
	for($i = 0; $i < $file_count; $i++)
	{
		$xml_parser = new get_xml_data();
		$xml_data[$i] = $xml_parser->open_file($array_data_path[$i]);
	}
	
	//array_data[path][attribute][1440]
	//initial data array
	for($i = 0; $i < $file_count; $i++)
	{
		for($j = 0; $j < $array_attribute_count; $j++)
		{
		    for($k = 0; $k < $array_attribute_data_count; $k++)
		    {
			    $array_data[$i][$j][$k] = null;
		    }
		}
	}
	
	for($i = 0; $i < $file_count; $i++)
	{
		$data_count = $xml_data[$i]->count();
		$file_name[$i] = (string)$xml_data[$i]['board_id'];
		$max_temeprature = 0;
		$mix_temperatre = 100;
		$max_humidity = 0;
		$min_humidity = 100;
		$max_lux = 0;
		$min_lux = 99999;
		
		
		for($j = 0; $j < $data_count; $j++)
		{
			$time = (string)$xml_data[$i]->data[$j]['time'];
			//$id = (string)$xml_data[$i]->data[$j]['id'];
			$devide_time = explode(":", $time);
			$hour = $devide_time[0];
			$minute = $devide_time[1];
			$num = 60 * $devide_time[0] + $devide_time[1];
			//[0]=temperature, [1]=humidity, [2]=lux, [3]=total_lux_sum
			$get_temperature = (string)$xml_data[$i]->data[$j]['T'];
			$get_humidity = (string)$xml_data[$i]->data[$j]['H'];
			$get_lux = (string)$xml_data[$i]->data[$j]['L'];
		    $get_lux_sum = (int)$xml_data[$i]->data[$j]['LS'];
	        
	        //attention: modify temperature
	        if($file_name[$i] == "WA008")//building J
	        {
	            $get_temperature -= 1;//decrease one degree
	        }
	        if($file_name[$i] == "WA001")//building C
	        {
	            $get_temperature -= 1.6;//decrease one degree
	        }
	        
	        //use the hundred as lux minimal unit 
	        if($get_lux > 1000)
	        {
                $get_lux = round($get_lux, -2);
	        }
	        
	        if($get_lux_sum == null)
	        {
	            $total_lux_sum += 0;
	        }
	        else
	        {
	            $total_lux_sum += $get_lux_sum;
	        }
	        
	        $array_data[$i][0][$num] = $get_temperature;
	        $array_data[$i][1][$num] = $get_humidity;
	        $array_data[$i][2][$num] = $get_lux;
	        $array_data[$i][3][$num] = round($total_lux_sum * $PPFD, 2);
	        
	        //find the max, min and current temperature, humidity and lux datapicker.
	        if($get_temperature > $max_temeprature)
	        {
	            $max_temeprature = $get_temperature;
	        }
	        if($get_temperature < $mix_temperatre)
	        {
	            $mix_temperatre = $get_temperature;
	        }
	        if($get_humidity > $max_humidity)
	        {
	            $max_humidity = $get_humidity;
	        }
	        if($get_humidity < $min_humidity)
	        {
	            $min_humidity = $get_humidity;
	        }
	        if($get_lux > $max_lux)
	        {
	            $max_lux = $get_lux;
	        }
	        
	        
	        //$array_current_data = [file][data attribute][data content]
	        //[file] = WA001:0 , WA002:1, ...
	        //[data attribute] = 0:temperatre 1:humidity 2:lux
	        //[data content] = 0:current 1:min(In lux is accumulation of lux) 2:max
	        
	        if($get_temperature != null)
	        {
	            $array_current_data[$i][0][0] = $get_temperature;
	        }
	        if($get_humidity != null)
	        {
	            $array_current_data[$i][1][0] = $get_humidity;
	        }
	        if($get_lux != null)
	        {
	            $array_current_data[$i][2][0] = $get_lux;
	        }
	        
	        $array_current_data[$i][0][1] = $mix_temperatre;
	        $array_current_data[$i][0][2] = $max_temeprature;
	        $array_current_data[$i][1][1] = $min_humidity;
	        $array_current_data[$i][1][2] = $max_humidity;
	        $array_current_data[$i][2][1] = round($total_lux_sum * $PPFD, 2);
	        $array_current_data[$i][2][2] = $max_lux;
	        
		}
		$get_temperature = 0;
		$get_humidity = 0;
		$get_lux = 0;
		$total_lux_sum = 0;
		$max_temeprature = 0;
		$mix_temperatre = 100;
		$max_humidity = 0;
		$min_humidity = 100;
		$max_lux = 0;
	}
	
	for($i = 0; $i < sizeof($file_name); $i++)
	{
	    if( $file_name[$i] == "WA001")
	        $file_name[$i] = "*C棟";
	    if( $file_name[$i] == "WA002")
	        $file_name[$i] = "新冷房";
	    if( $file_name[$i] == "WA003")
	        $file_name[$i] = "F棟";
	    if( $file_name[$i] == "WA004")
	        $file_name[$i] = "農場室外";
	    if( $file_name[$i] == "WA005")
	        $file_name[$i] = "舊冷房";
	    if( $file_name[$i] == "WA006")
	        $file_name[$i] = "H棟";
	    if( $file_name[$i] == "WA007")
	        $file_name[$i] = "I棟";
	    if( $file_name[$i] == "WA008")
	        $file_name[$i] = "*J棟";
	    if( $file_name[$i] == "WATEST")
	        $file_name[$i] = "家";
	}
	
?>
<html>
<head>
    <title>大奇蘭園</title>
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8">
    <script src="https://ajax.googleapis.com/ajax/libs/jquery/1.11.3/jquery.min.js"></script>
    <script src="http://code.highcharts.com/highcharts.js"></script>
    <!-- export highchart function-->
    <script src="http://code.highcharts.com/modules/exporting.js"></script>
    <script src="http://highcharts.github.io/export-csv/export-csv.js"></script>
    <!-- export highchart function-->
    <!-- load datapicker UI-->
    <link rel="stylesheet" href="//code.jquery.com/ui/1.11.4/themes/smoothness/jquery-ui.css">
    <script src="//code.jquery.com/ui/1.11.4/jquery-ui.js"></script>
    <link rel="stylesheet" href="/resources/demos/style.css">
    <!-- load datapicker UI-->
    
    <script type="text/javascript">
    $(function() {
        //load datapicker UI
        $("#datepicker").datepicker(
        {
            defaultDate: new Date(),
            
            onSelect: function()
            { 
                /*
                var date = $(this).datepicker('getDate'); 
                
                load_highcharts(year+month+day);
                */
                $(this).parent('form').submit();
            }
            
        });
        
        load_highcharts();
    });
    
    function load_highcharts() 
    {
        var interval = 2;//how much time to show the data (minute)
        var year = <?php echo $year;?>;
        var month = <?php echo $month;?>;
        var day = <?php echo $day;?>;
        //array_data[path][attribute][1440] attribute=temperature, humidity, lux, total_lux_sum
	    var array_file_count = <?php echo $file_count;?>;
	    var array_attribute_count = <?php echo $array_attribute_count;?>;
	    var array_attribute_data_count = <?php echo $array_attribute_data_count;?>;
	    //var array_lux_sum = <?php echo $file_count;?>;
	    //contain 1440 data.
	    var array_data = new Array(array_file_count);
	    //contain data to show on hightcahrt, filter with parameter interval
	    var array_highchart_data = new Array(array_file_count);
	    
	    //initial array
        for(var i = 0; i < array_file_count; i++)
        {
            array_data[i] = new Array(array_attribute_count);
            array_highchart_data[i] = new Array(array_attribute_count);
            for(var j = 0; j< array_attribute_count; j++)
            {
                array_data[i][j] = new Array(array_attribute_data_count);
                array_highchart_data[i][j] = new Array(array_attribute_data_count/interval);
                //array_lux_sum[i] = new Array(array_attribute_data_count);
            }
        }
	   
	    <?php
	    //trandfer php data to javascript
	    for($i = 0; $i < count($array_data); $i++)
	    {
	        for($j = 0; $j < count($array_data[$i]); $j++)
	        {
	            for($k = 0; $k < count($array_data[$i][$j]); $k++)
	            {
	                $data = $array_data[$i][$j][$k];
	                if(null != $data)
	                {
	                    echo "array_data[".$i."][".$j."][".$k."] = ".$data.";";
	                }
	                else
	                {
	                    //initial the array to 'null', or the highcharts wiil display fail.
	                    echo "array_data[".$i."][".$j."][".$k."] = null;";
	                }
	            }
	        }
	    }
        ?>
       
        //filter data to highcharts
        var normal_time = 0;//from 0 to 86400 it's a day's second time
        var interval_time = 0;
        for(var i = 0; i < array_file_count; i++)
        {
            for(var j = 0; j < array_attribute_count; j++)
            {
                for(var hour = 0; hour < 24; hour++)
                {
                    for(var minute = 0; minute < 60; minute++)
                    {
                        if(0 == minute % interval)
                        {
                            array_highchart_data[i][j][interval_time] = array_data[i][j][normal_time];
                            //array_lux_sum[i] += array_data[i][3][normal_time];
                            interval_time++;
                        }
                        normal_time++;
                    }
                }//end of each data item
                interval_time = 0;
                normal_time = 0;
            }
            
        }
        
        //add lux sum to array
        for(var i = 0; i < array_file_count; i++)
        {
            
            //array_lux_sum
        }
        
        
        
        //load temperature highcharts
        $('#temperature_container').highcharts({
            exporting: {
                filename: '<?php  echo $year."/".$month."/".$day; ?>_temperature'
            },
            chart: { type: 'spline' },
            title: { text: '大奇蘭園溫室溫度表', style:{ fontSize: '25px' } },
            subtitle: { text: '資料日期: ' + year + '年' + month + '月' + day + '日' },
            xAxis: {
                type: 'datetime',
                labels: { overflow: 'justify' }
            },
            yAxis: {
                title: { text: '攝氏 (°C)' },
                min: 0,
                minorGridLineWidth: 0,
                gridLineWidth: 0,
                alternateGridColor: null,
                plotBands: [
                { 
                    from: 0,
                    to: 13,
                    color: 'rgba(68, 170, 213, 0.1)',
                    label: { text: '寒害 <13°C',
                             style: { color: '#606060' }
                    }
                }, 
                { 
                    from:14,
                    to: 16,
                    color: 'rgba(0, 0, 0, 0)',
                    label: { text: '生長緩慢 14-16°C',
                             style: { color: '#606060' }
                    }
                }, 
                { 
                    from: 18,
                    to: 25,
                    color: 'rgba(68, 170, 213, 0.1)',
                    label: { text: '生殖生長 18-25°C',
                             style: { color: '#606060' }
                    }
                }, 
                { 
                    from:26,
                    to: 32,
                    color: 'rgba(0, 0, 0, 0)',
                    label: { text: '營養生長 26-32°C',
                             style: { color: '#606060' }
                    }
                }, 
                { 
                    from: 32,
                    to: 40,
                    color: 'rgba(68, 170, 213, 0.1)',
                    label: {
                        text: '日燒 <32°C',
                        style: {
                            color: '#606060'
                        }
                    }
                }]
            },
            tooltip: {
                valueSuffix: ' °C'
            },
            plotOptions: {
                spline: {
                    lineWidth: 2,
                    states: {
                        hover: {
                            lineWidth: 5
                        }
                    },
                    marker: {
                        enabled: false//每個資料顯示一個point
                    },
                    pointInterval: 60000 * interval,//hour=>3600000  minute=>60000  5minute=>300000
                    pointStart: Date.UTC(year, month-1, day, 0, 0, 0)//年月日時分秒
                }
            },
            series: [
            <?php
            for($i = 0 ; $i < $file_count; $i++)
            {
                echo "{";
                echo "name: \"".$file_name[$i]."\",";
                echo "data: array_highchart_data[".$i."][0]";
                echo "},";
            }
            ?>
            ],
            navigation: {
                menuItemStyle: {
                    fontSize: '10px'
                }
            }
          
        });//end of highcharts object
        
        //load humidity highcharts
        $('#humidity_container').highcharts({
            exporting: {
                filename: '<?php  echo $year."/".$month."/".$day; ?>_humidity'
            },
            chart: { type: 'spline' },
            title: { text: '大奇蘭園溫室濕度表', style:{ fontSize: '25px' }},
            subtitle: { text: '資料日期: ' + year + '年' + month + '月' + day + '日' },
            xAxis: {
                type: 'datetime',
                labels: { overflow: 'justify' }
            },
            yAxis: {
                title: { text: '百分比 (%)' },
                min: 30,
                max: 100,
                minorGridLineWidth: 0,
                gridLineWidth: 0,
                alternateGridColor: null,
                plotBands: [
                { 
                    from: 30,
                    to: 49,
                    color: 'rgba(68, 170, 213, 0.1)',
                    label: { text: '乾燥',
                             style: { color: '#606060' }
                    }
                }, { 
                    from:50,
                    to: 80,
                    color: 'rgba(0, 0, 0, 0)',
                    label: { text: '適合',
                             style: { color: '#606060' }
                    }
                }, 
                
                {
                    from: 81,
                    to: 100,
                    color: 'rgba(68, 170, 213, 0.1)',
                    label: {
                        text: '潮濕',
                        style: {
                            color: '#606060'
                        }
                    }
                }]
            },
            tooltip: {
                valueSuffix: ' %'
            },
            plotOptions: {
                spline: {
                    lineWidth: 2,
                    states: {
                        hover: {
                            lineWidth: 5
                        }
                    },
                    marker: {
                        enabled: false//每個資料顯示一個point
                    },
                    pointInterval: 60000 * interval,//hour=>3600000  minute=>60000  5minute=>300000
                    pointStart: Date.UTC(year, month-1, day, 0, 0, 0)//年月日時分秒
                }
            },
            series: [
            <?php
            for($i = 0 ; $i < $file_count; $i++)
            {
                echo "{";
                echo "name: \"".$file_name[$i]."\",";
                echo "data: array_highchart_data[".$i."][1]";
                echo "},";
            }
            ?>
            ],
            navigation: {
                menuItemStyle: {
                    fontSize: '10px'
                }
            }
        });//end of highcharts object
        
        //load lux highcharts
        $('#lux_container').highcharts({
            exporting: {
                filename: '<?php  echo $year."/".$month."/".$day; ?>_lux'
            },
            chart: { type: 'spline' },
            title: { text: '大奇蘭園溫室光度表', style:{ fontSize: '25px' } },
            subtitle: { text: '資料日期: ' + year + '年' + month + '月' + day + '日' },
            xAxis: {
                type: 'datetime',
                labels: { overflow: 'justify' }
            },
            yAxis: {
                title: { text: 'Lux (lx)' },
                min: 0,
                minorGridLineWidth: 0,
                gridLineWidth: 0,
                alternateGridColor: null,
                plotBands: [
                { 
                    from: 0, to: 5000,
                    color: 'rgba(68, 170, 213, 0.1)',
                    label: { text: '0~5K', style: { color: '#606060' }}
                }, 
                { 
                    from:5001, to: 10000,
                    color: 'rgba(0, 0, 0, 0)',
                    label: { text: '5~10k', style: { color: '#606060' }}
                }, 
                
                {
                    from: 10001, to: 15000,
                    color: 'rgba(68, 170, 213, 0.1)',
                    label: { text: '10k~15k', style: { color: '#606060'}}
                },
                {
                    from: 15001, to: 20000,
                    color: 'rgba(0, 0, 0, 0)',
                    label: { text: '15k~20k', style: { color: '#606060'}}
                },
                {
                    from: 20001, to: 25000,
                    color: 'rgba(68, 170, 213, 0.1)',
                    label: { text: '20k~25k', style: { color: '#606060'}}
                },
                {
                    from: 25001, to: 30000,
                    color: 'rgba(0, 0, 0, 0)',
                    label: { text: '25k~30k', style: { color: '#606060'}}
                },
                ]
            },
            tooltip: {
                valueSuffix: ' lx'
            },
            plotOptions: {
                spline: {
                    lineWidth: 2,
                    states: {
                        hover: {
                            lineWidth: 5
                        }
                    },
                    marker: {
                        enabled: false//每個資料顯示一個point
                    },
                    pointInterval: 60000 * interval,//hour=>3600000  minute=>60000  5minute=>300000
                    pointStart: Date.UTC(year, month-1, day, 0, 0, 0)//年月日時分秒
                }
            },
            series: [
            <?php
            for($i = 0 ; $i < $file_count; $i++)
            {
                echo "{";
                echo "name: \"".$file_name[$i]."\",";
                echo "data: array_highchart_data[".$i."][2]";
                echo "},";
            }
            ?>
            ],
            navigation: {
                menuItemStyle: {
                    fontSize: '10px'
                }
             }
        });//end of highcharts object
        
        //load lux sum highcharts
        $('#lux_sum_container').highcharts({
        exporting: {
                filename: '<?php  echo $year."/".$month."/".$day; ?>_lux_accmulation'
        },
        chart: { type: 'spline' },
        title: { text: '大奇蘭園光累積表', style:{ fontSize: '25px' } },
        subtitle: { text: '資料日期: ' + year + '年' + month + '月' + day + '日' },
        xAxis: {
            type: 'datetime',
            labels: { overflow: 'justify' }
        },
        yAxis: {
            title: {
                text: 'PPFD(μmol/m2/day)'
            },
            labels: {
                formatter: function () {
                    return this.value;
                }
            }
        },
        tooltip: {
            valueSuffix: ' μmol/m2/day'
        },
        plotOptions: {
            spline: {
                lineWidth: 2,
                states: {
                    hover: {
                        lineWidth: 5
                    }
                },
                marker: {
                    enabled: false//每個資料顯示一個point
                },
                pointInterval: 60000 * interval,//hour=>3600000  minute=>60000  5minute=>300000
                pointStart: Date.UTC(year, month-1, day, 0, 0, 0)//年月日時分秒
            }
        },
        series: [
        <?php
            for($i = 0 ; $i < $file_count; $i++)
            {
                echo "{";
                echo "name: \"".$file_name[$i]."\",";
                echo "data: array_highchart_data[".$i."][3]";
                echo "},";
            }
            ?>
        ]
    });
        //end of highcharts object
        /*
        $('#temperature_statement').empty();
        $('#humidity_statement').empty();
        $('#lux_statement').empty();
        $('#temperature_statement').append("目前溫度:" + get_temperature 
        + "℃, 最高溫度:" + max_temperature
        + "℃, 最低溫度:" + min_temperature + "℃");
        
        $('#humidity_statement').append("目前濕度:" + get_humidity 
        + "%, 最高濕度:" + max_humidity
        + "%, 最低濕度:" + min_humidity + "%");
        
        $('#lux_statement').append("目前照度:" + get_lux 
        + "lx, 最高照度:" + max_lux
        + "lx, 最低照度:" + min_lux + "lx");
        */
    }
</script>
</head>

<body id="body">
    <span style="margin:50px; font-size:35px; border: 1px solid 000000;">
        <a href="dachi_statistics.php" >前往統計報表網頁</a>
        <a href="dachi_environment_simple.php" >前往簡易版網頁</a>
    </span>
    <div id="select_date" style="text-align: right; margin-right:60">
    <form action="dachi_environment.php" method="post"> 
        Date: <input type="text" id="datepicker" name="datepicker" value="<?php echo $year."/".$month."/".$day; ?>"/>
    </form>
    
    </div>
    <?php
        if($date_exist == false)
        {
            echo "查無".$year."/".$month."/".$day."的資料!";
        }
    ?>
    <style type="text/css"> 
  
    .C_tr {
        display: table-row;
        vertical-align:middle;
    }
    .C_td {
        border:1px solid #000;
        display: table-cell;
        vertical-align:middle;
    }
    
    </style> 
    <div style="text-align: center;margin: 0px auto;font-size:25px;">
     <form action="dachi_environment.php" method="post"> 
         <input style="width:100px; font-size:25px" type="submit" name="button_select_date" value="前一天" />
         <input style="width:100px; font-size:25px" type="submit" name="button_select_date" value="今天" />
         <input style="width:100px; font-size:25px" type="submit" name="button_select_date" value="明天" />
         <input type="hidden" name="submit_date" value="<?php echo $year."/".$month."/".$day; ?>" />
         查詢時間:<?php echo $year."/".$month."/".$day." ".date("H").":".date("i"); ?>
    </form>
   
    </div>
    <div id="data_list" style="display:table; text-align: center; margin: 0px auto; height: 600px; font-size:28px;">
        <div class="C_tr">
            <div class="C_td">棟別</div>
            <div class="C_td">目前<br/>溫度</div>
            <div class="C_td">目前<br/>濕度</div>
            <div class="C_td">目前<br/>亮度</div>
            <div class="C_td">最高<br/>溫度</div>
            <div class="C_td">最低<br/>溫度</div>
            
            <div class="C_td">最高<br/>濕度</div>
            <div class="C_td">最低<br/>濕度</div>
            
            <div class="C_td">最高<br/>亮度</div>
            <div class="C_td">日累積PPFD<br/>(μmol/m2/day)</div>
            
            </div>
            <?php
            for($i = 0; $i < $file_count; $i++)
	       {
            ?>
            <div class="C_tr">
                <div class="C_td"><?php echo $file_name[$i]; ?></div>
                <div class="C_td"><?php echo $array_current_data[$i][0][0]; ?> <br/>°C</div>
                <div class="C_td"><?php echo $array_current_data[$i][1][0]; ?> <br/>%</div>
                <div class="C_td"><?php echo $array_current_data[$i][2][0]; ?> <br/>lx</div>
               
                <div class="C_td"><?php echo $array_current_data[$i][0][2]; ?> <br/>°C</div>
                <div class="C_td"><?php echo $array_current_data[$i][0][1]; ?> <br/>°C</div>
                
                <div class="C_td"><?php echo $array_current_data[$i][1][2]; ?> <br/>%</div>
                <div class="C_td"><?php echo $array_current_data[$i][1][1]; ?> <br/>%</div>
                
                <div class="C_td"><?php echo $array_current_data[$i][2][2]; ?> <br/>lx</div>
                <div class="C_td"><?php echo $array_current_data[$i][2][1]; ?> <br/>μmol</div>
                
            </div>
            <?php 
            }
            ?>
                
        </div>
    </div>
    <div id="highcharts_area">
        <div>
            <div id="temperature_container" style="min-width: 310px; height: 600px; margin: 0 auto">  </div>
        <div>
            <div id="humidity_container" style="min-width: 310px; height: 600px; margin: 0 auto">  </div>
        </div>
        <br />
        <div>
            <div id="lux_container" style="min-width: 310px; height: 600px; margin: 0 auto">  </div>
        </div>
        <br />
        <div>
            <div id="lux_sum_container" style="min-width: 310px; height: 600px; margin: 0 auto">  </div>
        </div>
    </div>
    <div id="debug"></div>
    
</body>
</html>

