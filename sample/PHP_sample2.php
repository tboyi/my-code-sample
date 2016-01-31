

<?php
    date_default_timezone_set("Asia/Taipei");//set up time zone or the default time will decrease 8 hour.
    include("./get_xml_data.php");
    //$select_year = 2015;
    //$select_month = 12;
    $file_name;//save id name
	$file_count = 0;
	$PPFD = 0.017 / 86400;// the lux to ppfd formula (Lux to PPF (μmol m-2 s-1))
    $database_directory_path = "./database/";
    $max_day_count = 0;//find the max day in selected month
    $monthly_sun_light_accumulate = 0;
    $get_date = $_POST['datepicker'];
    $button_select_date = $_POST['button_select_date'];
    $date_exist = true;
    $statistics_array;
    $max_temperature = 0;
    $min_temperature = 99;
    
    if(null == $get_date)
	{
	    $datepicker_year = date("Y");
	    $datepicker_month = date("m");
	}
	else
	{
	    $dates = explode(" ", $get_date);
        $datepicker_month = $dates[0]; 
        $datepicker_year = $dates[1];
	}
	
	if ($button_select_date == "今天") {
        $datepicker_year = date("Y");
	    $datepicker_month = date("m");
    }
    
    //echo $datepicker_month."<br/>";
    //echo $datepicker_year."<br/>";
    
    $select_year = $datepicker_year;
    $select_month = $datepicker_month;
    
    //$statistics_array[file ID][function][function content]
    //statistics_array[WA001...WA00X][0][0] = [id]
    //statistics_array[WA001...WA00X][0][1] = [name]
    //statistics_array[WA001...WA00X][1][0~max days] = [daily light accumulate]
	//statistics_array[WA001...WA00X][2][0~max days] = [monthly light accumulate]
    //statistics_array[WA001...WA00X][3][0~max days] = [min temperature]
	//statistics_array[WA001...WA00X][4][0~max days] = [max temperature]
    
   
    
    $files = scandir($database_directory_path);
    
    //scan how many file are there
	for($i = 0; $i < count($files) - 2 ; $i++)
	{
	    $file_directory_path = $database_directory_path.$files[$i+2]."/";
	    //echo $files[$i+2]."<br/>";//WA001...WA00x
	    
	    $data_monthly_files = scandir($file_directory_path);
	    //into date directory ex: WA001/201511/
	    for($j = 0; $j < count($data_monthly_files) - 2 ; $j++)
	    {
	        $monthly_file_path = $database_directory_path.$files[$i+2]."/".$data_monthly_files[$j+2]."/";
	        //echo "::  ".$monthly_file_path."<br/>";
	        
	        $data_daily_files = scandir($monthly_file_path);
	        $monthly_sun_light_accumulate = 0;
	        
	        //into date file. ex: WA001/201511/20151103.xml
	        for($k = 0; $k < count($data_daily_files) - 2 ; $k++)
	        {
	            $daily_file_path = $database_directory_path.$files[$i+2]."/".$data_monthly_files[$j+2]."/".$data_daily_files[$k+2];
	            $date = substr($data_daily_files[$k+2], 6, 2);//substr("20151128.xml", 6, 2); to 28
	            //echo $date."<br/>";
	            //echo "add file:".$daily_file_path."<br/>";
	            if($select_year.$select_month == $data_monthly_files[$j+2])
	            {
	                $daily_sun_light_accumulate = 0;
	                $max_temperature = 0;
	                $min_temperature = 99;
	                //echo "add file:".$daily_file_path."<br/>";    
	                $xml_parser = new get_xml_data();
		            $xml_data = $xml_parser->open_file($daily_file_path);
		            $data_count = $xml_data->count();
		            //echo $data_count."<br/>";
		            for($l = 0; $l < $data_count; $l++)
		            {
		                $get_lux_sum = (int)$xml_data->data[$l]['LS'];
		                $get_temperature = (string)$xml_data->data[$l]['T'];
		                
		                //echo $get_lux_sum."<br/>"; 
	                    if($get_lux_sum == null)
	                    {
	                        $daily_sun_light_accumulate += 0;
	                        $monthly_sun_light_accumulate += 0;
	                    }
	                    else
	                    {
	                        $daily_sun_light_accumulate += $get_lux_sum;
	                        $monthly_sun_light_accumulate += $get_lux_sum;
	                    }
	                    
	                    if($get_temperature > $max_temperature)
	                    {
	                        $max_temperature = $get_temperature;
	                    }
	                    
		                if($get_temperature < $min_temperature)
	                    {
	                        $min_temperature = $get_temperature;
	                    }
		            }//end of daily data loop
		            
		            //if must use $file_count as array index. 
		            //If use $i, it will add the empty file directory as index, and cause the wrong file index.
		            $statistics_array[$file_count][0][0] = $files[$i+2];
		            //because the highcharts linitation, use the second digit after the decimal point.
		            $statistics_array[$file_count][1][$date] = round($daily_sun_light_accumulate * $PPFD, 1);
		            $statistics_array[$file_count][2][$date] = round($monthly_sun_light_accumulate * $PPFD, 0);
		            $statistics_array[$file_count][3][$date] = $min_temperature;
		            $statistics_array[$file_count][4][$date] = $max_temperature;
		            //echo "[".$file_count."][".$date."]".$total_lux_sum."<br/>";
		            
		            //if the current data is not completely monthly data, use the max date as date count.
		            if($max_day_count < $date)
		            {
		                $max_day_count = $date;
		            }
	            }
	            
	        }//end of daily file loop
	        
	        //if selected file has data in selected month, use it as one of the index.
	        if(count($data_daily_files) > 2 && $select_year.$select_month == $data_monthly_files[$j+2])
	        {
	            //$file_name[$file_count] = $files[$i+2];
	            $file_count++;
	        }
	    }
	}
	
    for($i = 0; $i < $file_count; $i++)
	{
	    if( $statistics_array[$i][0][0] == "WA001")
	    {
	        $statistics_array[$i][0][1] = "C棟";
	    }
	    if( $statistics_array[$i][0][0] == "WA002")
	    {
	        $statistics_array[$i][0][1] = "新冷房";
	    }
	    if( $statistics_array[$i][0][0] == "WA003")
	    {
	        $statistics_array[$i][0][1] = "F棟";
	    }
	    if( $statistics_array[$i][0][0] == "WA004")
	    {
	        $statistics_array[$i][0][1] = "農場室外";
	    }
	    if( $statistics_array[$i][0][0] == "WA005")
	    {
	        $statistics_array[$i][0][1] = "舊冷房";
	    }
	    if( $statistics_array[$i][0][0] == "WA006")
	    {
	        $statistics_array[$i][0][1] = "H棟";
	    }
	    if( $statistics_array[$i][0][0] == "WA007")
	    {
	        $statistics_array[$i][0][1] = "I棟";
	    }
	    if( $statistics_array[$i][0][0] == "WA008")
	    {
	        $statistics_array[$i][0][1] = "J棟";
	    }
	    if( $statistics_array[$i][0][0] == "WATEST")
	    {
	        $statistics_array[$i][0][1] = "家";
	    }
	}
	
	//echo '<pre>', print_r($statistics_array, true), '</pre>';
	
	if($file_count == 0)
	{
	    $date_exist = false;
	}
	else
	{
	    $date_exist = true;
	}
?>

<html>
<head>
    <title>大奇蘭園</title>
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8">
    <script src="https://ajax.googleapis.com/ajax/libs/jquery/1.11.3/jquery.min.js"></script>
    <script src="http://code.highcharts.com/highcharts.js"></script>
    <script src="https://code.highcharts.com/highcharts-more.js"></script>
    <!-- export highchart function-->
    <script src="http://code.highcharts.com/modules/exporting.js"></script>
    <script src="http://highcharts.github.io/export-csv/export-csv.js"></script>
    <!-- export highchart function-->
    <!-- load datapicker UI-->
    <link rel="stylesheet" href="//code.jquery.com/ui/1.11.4/themes/smoothness/jquery-ui.css">
    <script src="//code.jquery.com/ui/1.11.4/jquery-ui.js"></script>
    <link rel="stylesheet" href="/resources/demos/style.css">
    <!-- load datapicker UI-->
    
    
    <style type='text/css'>
    .ui-datepicker-calendar {
        display: none; /* redefine datepicker format*/
    }
    </style>
    
    <script type="text/javascript">
    $(function() {
        //load datapicker UI
        $("#datepicker").datepicker({
            defaultDate: new Date(),
            changeMonth: true,
            changeYear: true,
            showButtonPanel: true,
            dateFormat: 'MM yy',
            monthNames: ["1","2","3","4","5","6","7","8","9","10","11","12"],
            monthNamesShort: ["1","2","3","4","5","6","7","8","9","10","11","12"],
            
            onClose: function(dateText, inst) { 
                var month = $("#ui-datepicker-div .ui-datepicker-month :selected").val();
                var year = $("#ui-datepicker-div .ui-datepicker-year :selected").val();
                $(this).datepicker('setDate', new Date(year, month, 1));
                $(this).parent('form').submit();
            }
        });
       
        load_highcharts();
    });
    
    function load_highcharts() 
    {
        var file_count = <?php echo $file_count; ?>;
        var max_day_count = <?php echo $max_day_count; ?>;
        //initial array
        var statistics_array = new Array(file_count);
        
        for(var i = 0; i < file_count; i++)
        {
            statistics_array[i] = new Array(5);
        }
        
        
        for(var i = 0; i < file_count; i++)
        {
            statistics_array[i][0] = new Array(2);
            statistics_array[i][1] = new Array(max_day_count);
            statistics_array[i][2] = new Array(max_day_count);
            statistics_array[i][3] = new Array(max_day_count);
            statistics_array[i][4] = new Array(max_day_count);
        }
        
        <?php
        
	    //trandfer php data to javascript
	    //WA001/201512/20151201.xml => (php array)$daily_lux_accumulation[0][0][01]  => (javascript array)daily_lux_accumulation[0][0][0]
	    //WA001/201512/20151202.xml => $daily_lux_accumulation[0][0][02]
	    //WA001/201512/20151203.xml => $daily_lux_accumulation[0][0][03]
	    //...
	    //WA002/201512/20151201.xml => $daily_lux_accumulation[1][0][01]
	    
	    
	    //$statistics_array[file ID][function][function content]
        //statistics_array[WA001...WA00X][0][0] = [id]
        //statistics_array[WA001...WA00X][0][1] = [name]
        //statistics_array[WA001...WA00X][1][01~max days] = [daily light accumulate]
	    //statistics_array[WA001...WA00X][2][01~max days] = [monthly light accumulate]
        //statistics_array[WA001...WA00X][3][01~max days] = [min temperature]
	    //statistics_array[WA001...WA00X][4][01~max days] = [max temperature]
	    for($i = 0; $i < $file_count; $i++)
	    {
	        echo "statistics_array[".$i."][0][0] = \"".$statistics_array[$i][0][0]."\";";
	        echo "statistics_array[".$i."][0][1] = \"".$statistics_array[$i][0][1]."\";";
	        for($j = 0; $j < $max_day_count; $j++)
	        {
	            if($j < 9)
	            {
	                $daily_lux_data = $statistics_array[$i][1]["0".($j+1)];
	                $monthly_lux_data = $statistics_array[$i][2]["0".($j+1)];
	                $min_temperature_data = $statistics_array[$i][3]["0".($j+1)];
	                $max_temperature_data = $statistics_array[$i][4]["0".($j+1)];
	            }
	            else
	            {
	                $daily_lux_data = $statistics_array[$i][1][$j+1];
	                $monthly_lux_data = $statistics_array[$i][2][$j+1];
	                $min_temperature_data = $statistics_array[$i][3][$j+1];
	                $max_temperature_data = $statistics_array[$i][4][$j+1];
	            }
	                
	            //initial the array to 'null', or the highcharts wiil display fail.
	            if(null != $daily_lux_data)
	            {
	                echo "statistics_array[".$i."][1][".$j."] = ".$daily_lux_data.";";
	            }
	            else
	            {
	                echo "statistics_array[".$i."][1][".$j."] = null;";
	            }
	            if(null != $monthly_lux_data)
	            {
	                echo "statistics_array[".$i."][2][".$j."] = ".$monthly_lux_data.";";
	            }
	            else
	            {
	                echo "statistics_array[".$i."][2][".$j."] = null;";
	            }
	            if(null != $min_temperature_data)
	            {
	                echo "statistics_array[".$i."][3][".$j."] = ".$min_temperature_data.";";
	            }
	            else
	            {
	                echo "statistics_array[".$i."][3][".$j."] = null;";
	            }
	            if(null != $max_temperature_data)
	            {
	                echo "statistics_array[".$i."][4][".$j."] = ".$max_temperature_data.";";
	            }
	            else
	            {
	                echo "statistics_array[".$i."][4][".$j."] = null;";
	            }
	            
	        }
	    }
	    
        ?>
        
        //transform temperature data from phph array to javascript array
        var temperature_array = new Array(file_count);
        
        for(var i = 0; i < file_count; i++)
        {
            temperature_array[i] = new Array(max_day_count);
        }
        
        for(var i = 0; i < file_count; i++)
        {
            for(var j = 0; j < max_day_count; j++)
            {
                temperature_array[i][j] = new Array(2);
            }
        }
        
        for(var i = 0; i < file_count; i++)
        {
            for(var j = 0; j < max_day_count; j++)
            {
                temperature_array[i][j][0] = statistics_array[i][3][j];
                temperature_array[i][j][1] = statistics_array[i][4][j];
            }
        }
        
        
        
        //debug
        <?php 
        for($i = 0; $i < $file_count; $i++)
	    {
	    ?>
	        //debug.innerHTML = debug.innerHTML + statistics_array[<?php //echo $i; ?>][0] +"<br/>";
            //debug.innerHTML = debug.innerHTML + statistics_array[<?php //echo $i; ?>][1] +"<br/>";
            //debug.innerHTML = debug.innerHTML + statistics_array[<?php //echo $i; ?>][2] +"<br/>";
            //debug.innerHTML = debug.innerHTML + statistics_array[<?php //echo $i; ?>][3] +"<br/>";
            //debug.innerHTML = debug.innerHTML + statistics_array[<?php //echo $i; ?>][4] +"<br/>";
            //debug.innerHTML = debug.innerHTML + "=======<br/>";
	    <?php 
	    }
	    ?>
	    
	    //debug
        $('#daily_ligh_statistics_container').highcharts({
        exporting: {
            filename: '<?php  echo $select_year."/".$select_month; ?>_daily_ligh_statistics'
        },
        chart: {
            type: 'column'
        },
        title: {
            text: '大奇蘭園每日照度統計圖', style:{ fontSize: '25px' }
        },
        subtitle: {
            text: '資料日期: ' + <?php  echo $select_year; ?> + '年' + <?php  echo $select_month; ?> + '月'
        },
        xAxis: {
            categories: [
                <?php
                for($i = 0; $i < $max_day_count; $i++)
                {
                    echo "'".$select_month."/".($i+1)."',";
                }
                ?>
            ],
            crosshair: true
        },
        yAxis: {
            min: 0,
            title: {
                text: 'PPFD (μmol/m2/day)'
            }
        },
        tooltip: {
            headerFormat: '<span style="font-size:20px">{point.key}</span><table>',
            pointFormat: '<tr><td style="color:{series.color};padding:0">{series.name}: </td>' +
                '<td style="padding:0"><b style="font-size:20px">{point.y:.1f} μmol</b></td></tr>',
            footerFormat: '</table>',
            shared: true,
            useHTML: true
        },
        plotOptions: {
            column: {
                pointPadding: 0.2,
                borderWidth: 0
            }
        },
        series: [
        <?php
        for($i = 0; $i < $file_count; $i++)
        {
            echo "{";
            echo "name:'".$statistics_array[$i][0][1]."',";
            echo "data:statistics_array[".$i."][1]";
            echo "},";
        }
        ?>
        ]
    });
    
    //load monthly sun accumulation highcharts
    $('#daily_ligh_accmulation_statistics_container').highcharts({
        exporting: {
            filename: '<?php  echo $select_year."/".$select_month; ?>_daily_ligh_accmulation_statistics'
        },
        chart: {
            type: 'line'
        },
        title: {
            text: '大奇蘭園每日照度累進統計圖', style:{ fontSize: '25px' }
        },
        subtitle: {
            text: '資料日期: ' + <?php  echo $select_year; ?> + '年' + <?php  echo $select_month; ?> + '月'
        },
        xAxis: {
            categories: [
            <?php
            for($i = 0; $i < $max_day_count; $i++)
            {
                echo "'".$select_month."/".($i+1)."',";
            }
            ?>
            ]
        },
        yAxis: {
            title: {
                text: 'PPFD (μmol/m2/day)'
            }
        },
        plotOptions: {
            line: {
                dataLabels: {
                    enabled: true
                },
                enableMouseTracking: false
            }
        },
        series: [
        <?php
        for($i = 0; $i < $file_count; $i++)
        {
            echo "{";
            echo "name:'".$statistics_array[$i][0][1]."',";
            echo "data:statistics_array[".$i."][2]";
            echo "},";
        }
        ?>
        ]
    });
    //load temperature statistics load daily_temperature_statistics
  
    $('#daily_temperature_range_statistics_container').highcharts({
        exporting: {
            filename: '<?php  echo $select_year."/".$select_month; ?>_daily_temperature_range_statistics'
        },
        chart: {
            type: 'columnrange',
            inverted: true
        },

        title: {
            text: '大奇蘭園單日溫差統計表', style:{ fontSize: '25px' }
        },

        subtitle: {
            text: '資料日期: ' + <?php  echo $select_year; ?> + '年' + <?php  echo $select_month; ?> + '月'
        },

        xAxis: {
            categories: [
            <?php
            for($i = 0; $i < $max_day_count; $i++)
            {
                echo "'".$select_month."/".($i + 1)."',";
            }
            ?>
            ]
        },

        yAxis: {
            title: {
                text: '攝氏 ( °C )'
            }
        },

        tooltip: {
            valueSuffix: '°C'
        },

        plotOptions: {
            columnrange: {
                dataLabels: {
                    enabled: true,
                    formatter: function () {
                        return this.y + '°C';
                    }
                }
            }
        },

        legend: {
            enabled: true
        },
        
        series: [
            <?php
            for($i = 0; $i < $file_count; $i++)
            {
                echo "{";
                echo "name:'".$statistics_array[$i][0][1]."',";
                echo "data:temperature_array[".$i."]";
                echo "},";
            }
            ?>
        ]

    });
   
}//end of function load_highcharts
    
    </script>
    
</head>
<body id="body">
    <span style="margin:50px; font-size:35px; border: 1px solid 000000;">
        <a href="dachi_environment.php" >前往各棟溫室資料網頁</a> 
        <a href="dachi_environment_simple.php" >前往簡易版網頁</a>
    </span>
    <div id="select_date" style="text-align: right; margin-right:60">
       <form action="dachi_statistics.php" method="post"> 
        Date: <input type="text" id="datepicker" name="datepicker" value="<?php echo $datepicker_year."/".$datepicker_month; ?>"/>
    </form>
    <form action="dachi_statistics.php" method="post"> 
         <input type="submit" name="button_select_date" value="今天" style="width: 170px;"/>
         <input type="hidden" name="submit_date" value="<?php echo $datepicker_year."/".$datepicker_month; ?>" />
    </form>
    <?php
        if($date_exist == false)
        {
            echo "查無".$datepicker_year."/".$datepicker_month."的資料!";
        }
    ?>
    </div>
    <div id="daily_ligh_statistics_container" style="min-width: 310px; height: 600px; margin: 0 auto"></div>
    <div id="daily_ligh_accmulation_statistics_container" style="min-width: 310px; height: 600px; margin: 0 auto"></div>
    <div id="daily_temperature_range_statistics_container" style="min-width: 310px; height: <?php echo $file_count * $max_day_count * 20 ?>px; margin: 0 auto"></div>

    <div id="debug">
        
    </div>
</body>
</html>

