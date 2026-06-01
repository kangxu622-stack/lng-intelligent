<template>
    
        <div ref="theChart" :style="{height:height, width:width,padding:padding}"></div>
        <!-- <sc-dialog ref="settingDialogDom" draggable v-model="settingDialog" title="坐标轴配置" :width="600" :show-fullscreen="false" :show-close="true">
            <axissetting ref="setting"></axissetting>
            <template #footer>
                <el-button @click="settingDialog = false">取 消</el-button>
                <el-button type="primary" @click="settingAxis">确 定</el-button>
            </template>
        </sc-dialog> -->
    
    
</template>
<script>
import Highcharts from "highcharts";
import Highcharts3d from 'highcharts/highcharts-3d'
import HighchartsMore from 'highcharts/highcharts-more';
import loadExporting from "highcharts/modules/exporting";
import exportExcel from "highcharts/modules/export-data.src";
import offlineexport from "highcharts/modules/offline-exporting";
// import axissetting from "./axissetting";
import annotations from 'highcharts/modules/annotations.src' 
loadExporting(Highcharts);
exportExcel(Highcharts);
offlineexport(Highcharts);
HighchartsMore(Highcharts);
Highcharts3d(Highcharts)
annotations(Highcharts);
	export default {
		...Highcharts,
		name: "PdmHighcharts",
        components: {
			// axissetting
		},
		props: {
			height: { type: String, default: "100%" },
			width: { type: String, default: "100%" },
            padding:{type:String,default:'0px'},
			nodata: {type: Boolean, default: false },
			option: { type: Object, default: () => {}}
		},
		data() {
			return {
                settingDialog: false,
				isActivat: false,
				myChart: null,
                clickAxis:null
			}
		},       
		watch: {
			option: {
				deep:true,
				handler (v) {
                    this.myChart=new Highcharts.Chart(this.$refs.theChart, v);
					this.bindAxisClick();
				}
			}
		},
		computed: {
			myOptions: function() {
				return this.option || {};
			},
            defaultTheme:function(){
                return {
                        colors: ['#0687FF', '#ff631f', '#8bc813', '#ba55d3', '#be5028', '#21AD4B', '#00ffff', '#FFCD20', '#8b4513', '#c8b71a', '#fa00fa','#ACE8F8','#FFCD1F','#FBB367','#80B1D2','#FB8070','#CC99FF','#B0D961','#99CCCC','#BEBBD8','#FFCC99', '#8DD3C8','#FF9999','#CCEAC4','#BB81BC','#FBCCEC','#CCFF66','#99CC66','#66CC66','#FF6666','#FFED6F'],
                        lang: { 
                            numericSymbols: null, 
                            thousandsSep: '',
                            resetZoom:'重置',
                            resetZoomTitle:'重置比例 1:1',
                            contextButtonTitle:'图形操作',
                            viewFullscreen: "全屏显示",
                            exitFullscreen:'退出全屏',
                            downloadPDF: "下载PDF文件",
                            downloadPNG: "下载PNG图片",
                            downloadSVG: '下载矢量图片',
                            downloadCSV:"下载CSV文件",
                            downloadXLS:"下载XLS文件"
                         },
                        chart: {
                            spacingBottom:10,
                            spacingTop:10,
                            spacingLeft:10,
                            spacingRight:10,
                            type: 'line',
                            zoomType: 'x',
                            borderWidth: 0,
                            plotBorderWidth: 0
                        },
                        credits:{
                            enabled:false
                        },
                        exporting: {
                            filename: '下载数据',
							enabled: false,
                            url:'http://192.168.2.2/export',
                            buttons: {
                                contextButton: {
                                    menuItems: ['viewFullscreen','downloadPNG', 'downloadSVG', 'downloadXLS'],
                                    x: -10,
                                    y: 0
                                }
                            }
                        },
                        boost: {
                            useGPUTranslations: true
                        },
                        legend: {
                            enabled: true,
                            backgroundColor: (Highcharts.theme && Highcharts.theme.legendBackgroundColorSolid) || 'WhiteSmoke',
                            borderColor: '#CCC',
                            borderWidth: 1,
                            shadow: false
                        },
                        title: {
                            text:null,
                            style: {
                                color: '#000',
                                font: "bold 13px 'Helvetica Neue',Helvetica,'PingFang SC','Hiragino Sans GB','Microsoft YaHei','微软雅黑',Arial,sans-serif"
                            }
                        },
                        subtitle: {
                            style: {
                                color: '#666666',
                                font: "bold 12px 'Helvetica Neue',Helvetica,'PingFang SC','Hiragino Sans GB','Microsoft YaHei','微软雅黑',Arial,sans-serif"
                            }
                        },
                        accessibility: {
                            enabled: false
                        },
                        plotOptions: {
                            series: {
                                turboThreshold: 0,
                                connectNulls: false,
                                marker: {
                                    radius: 3
                                }
                            }
                        },
                        tooltip: {
                            valueDecimals: 2,
                        },
                        series: [],
                        xAxis: {
                            gridLineWidth: 1,
                            lineColor: '#000',
                            tickColor: '#000',
                            showLastLabel:true,
                            labels: {
                                style: {
                                    color: '#000',
                                    font: '11px Trebuchet MS, Verdana, sans-serif',
                                    cursor: 'point'
                                }
                            },
                            title: {
                                style: {
                                    color: '#333',
                                    fontWeight: 'bold',
                                    fontSize: '12px',
                                    fontFamily: 'Trebuchet MS, Verdana, sans-serif',
                                    cursor: 'point'
                                }
                            },
                            events: {
                                //setExtremes: this.syncExtremes
                            }
                        },
                        yAxis: {
                            minorTickInterval: 'auto',
                            lineColor: '#000',
                            lineWidth: 1,
                            tickWidth: 1,
                            tickColor: '#000',
                            showLastLabel:true,
                            labels: {
                                style: {
                                    color: '#000',
                                    font: '11px Trebuchet MS, Verdana, sans-serif',
                                    cursor:'point'
                                }
                            },
                            title: {
                                style: {
                                    color: '#333',
                                    fontWeight: 'bold',
                                    fontSize: '12px',
                                    fontFamily: 'Trebuchet MS, Verdana, sans-serif',
                                    cursor: 'point'
                                }
                            }
                        },
                        labels: {
                            style: {
                                color: '#99b'
                            }
                        },
                        navigation: {
                            buttonOptions: {
                                theme: {
                                    stroke: '#CCCCCC'
                                }
                            }
                        }
                }
            }
		},
		activated(){
			if(!this.isActivat){
				this.$nextTick(() => {
					this.myChart.reflow()
				})
			}
		},
		deactivated(){
			this.isActivat = false;
		},
		mounted(){
			this.isActivat = true;
			this.$nextTick(() => {
                Highcharts.setOptions(this.defaultTheme);
				this.draw();
			})
		},
		methods: {
			draw(options){
                let tempOptions=options!=undefined?Object.assign({},this.defaultTheme,options):this.myOptions;     
				this.myChart =new Highcharts.Chart(this.$refs.theChart, tempOptions);
                this.bindAxisClick();
				window.addEventListener('resize',function (){
					this.myChart.reflow();
				}.bind(this));
			},
            syncExtremes(e){
                var thisChart = this.myChart;
                if (e.trigger !== 'syncExtremes') {
                    Highcharts.charts.forEach(function (chart) {
                        if (chart !== thisChart && chart != undefined) {
                            if (chart.xAxis[0].setExtremes) {
                                chart.xAxis[0].setExtremes(e.min, e.max, true, false, { trigger: 'syncExtremes' });
                            }
                        }
                    });
                }
            },
            pointerDown(e){
                //var v=e.target;
                //console.log("触发了"+e);
                this.settingDialog=true;
                this.$nextTick(() => {
                    this.$refs.setting.setData(e);
			    })
                
                this.clickAxis=e;
            },
            bindAxisClick(){
                    //var row=this.myChart.axes[i];
                var vueDom=this;
                if(this.myChart.xAxis.length>0)
                {
                    for(var t=0;t<this.myChart.xAxis.length;t++)
                    {
                        let xAxis=this.myChart.xAxis[t];                        
                        xAxis['axisTitle']&&Highcharts.addEvent(xAxis.axisTitle.element, 'click',function(){vueDom.pointerDown(xAxis)});
                        xAxis['labelGroup']&&Highcharts.addEvent(xAxis.labelGroup.element, 'click',function(){vueDom.pointerDown(xAxis)});
                    }
                }
                if(this.myChart.yAxis.length>0)
                {
                    for(var k=0;k<this.myChart.yAxis.length;k++)
                    {
                        let yAxis=this.myChart.yAxis[k];
                        yAxis['axisTitle']&&Highcharts.addEvent(yAxis.axisTitle.element, 'click',function(){vueDom.pointerDown(yAxis)});
                        yAxis['labelGroup']&&Highcharts.addEvent(yAxis.labelGroup.element, 'click',function(){vueDom.pointerDown(yAxis)});
                    }                   
                }
            },
            settingAxis(){
                let data= this.$refs.setting.getData();
                this.clickAxis.update(data,true);
                this.settingDialog=false;
                this.bindAxisClick();
            },
            reflow(){
                if(this.myChart!=null)
                {
                    this.myChart.reflow();
                }
            },
		}
	}
</script>