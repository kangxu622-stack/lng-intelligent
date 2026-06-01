<template>
    <el-container style="height:100%;">
        <el-main class="nopadding">
            <el-row class="g-h100">
                <el-col class="g-h100" :span="12">
                    <div class="cardBorder" style="padding-left:0px;height:100%;">
                        <pagePanel headerTitle="宏观控制图" style="height:100%;margin:0px;" showBtn>
                            <PdmHighcharts ref="macorChartRef" height="calc(100% - 0px)" width="100%"
                                :option="hgkzOption" />
                        </pagePanel>
                    </div>
                </el-col>
                <el-col class="g-h100" :span="12">
                    <div class="cardBorder" style="padding-right:0px;">
                        <pagePanel headerTitle="层位注水变化趋势图" style="height:100%;margin:0px;" showBtn>
                            <PdmHighcharts ref="workPointChartRef" height="calc(100% - 0px)" width="100%"
                                :option="cwzsOption" />
                        </pagePanel>
                    </div>
                </el-col>
            </el-row>
        </el-main>
        <el-footer class="panelBox" height="200px" style="padding:0px;margin-top:5px;">
            <el-table :data="tableData" stripe border height="100%">
                <el-table-column label="层位" prop="productionIntervalName" align="center"></el-table-column>
                <el-table-column label="日期" prop="prodDate" align="center"></el-table-column>
                <el-table-column label="井口安全注入压力(MPa)" prop="wellHeaderSafePress" min-width="120" align="center"></el-table-column>
                <el-table-column label="配注完成率(%)" prop="allocInjRatio" align="center"></el-table-column>
                <el-table-column label="配注完成率变化" prop="allocInjRatiochange" align="center"></el-table-column>
                <el-table-column label="注入压力(MPa)" prop="whInjPress" align="center"></el-table-column>
                <el-table-column label="注入量(m³/d)" prop="injDaily" align="center"></el-table-column>
                <el-table-column label="配注量(m³/d)" prop="allocInjDaily" align="center"></el-table-column>
            </el-table>
        </el-footer>
    </el-container>
</template>

<script>
import scEcharts from '@/components/scEcharts/index.vue';
import PdmHighcharts from "@/components/zclt/PdmHighchart/index.vue";
import { InjectWaterMacroEvaluation} from '@/api/znzclt/zclt/sjpgfxyzd.js'
export default {
    name: '',
    data(){
        return {
            hgkzOption: {},
            cwzsOption: {},
            tableData: [],
            StyleColor:"white",
            wellId:'',
            platformId:'',
            ogfId:'',
            ProdDate:'',
            itemNum:null,
            addWaterForm:'',

        }
    },
    props:{
        ParamsData:{},
        NodeData:{},
        AddWaterForm:{},
    },
    watch: {
        '$store.state.setting.mode': {
            deep: true,//深度监听设置为 true
            immediate: true,
            handler: function (newVal, oldVal) {
                setTimeout(() => {
                    this.updateStyle(newVal);
                    this.LoadAll();
                }, 500);
            }
        },
        NodeData:{
            deep:true,//深度监听设置为 true
            immediate: true,
            handler:function(){
                let that = this;
                this.$nextTick( ()=>{
                    this.handleNodeClick(this.NodeData);
                })
            }
        },
        ParamsData:{
            deep:true,//深度监听设置为 true
            immediate: true,
            handler:function(){
                this.ProdDate = this.ParamsData;
            }
        },
        AddWaterForm:{
            deep:true,//深度监听设置为 true
            immediate: true,
            handler:function(){
                this.addWaterForm = this.AddWaterForm;
            }
        },

    },
    components: {
        PdmHighcharts,
        scEcharts
    },
    mounted() {
        this.LoadAll();
    },
    methods: {
        //加载页面数据
        LoadAll() {
            this.gethgkzOption(1);
            this.getcwzsOption(1);
        },
        updateTime(){
            if(this.itemNum  == null){
                return
            }
            else{
                this.getHongGuanChartData(this.itemNum);
            }
        },
        //树点击
        handleNodeClick(Node){
            if (Node.entityType == 10) {
                this.wellId = Node.id;
                this.ogfId = '';
                this.platformId = '';
                this.getHongGuanChartData(3);
                this.itemNum = 3;
            }else if(Node.entityType == 3){
                this.ogfId = Node.id;
                this.wellId = '';
                this.platformId = '';
                this.getHongGuanChartData(1)
                this.itemNum = 1;
            }else if(Node.entityType == 6){
                this.platformId = Node.id;
                this.wellId = '';
                this.ogfId = '';
                this.getHongGuanChartData(2);
                this.itemNum = 2;
            }

        },
        gethgkzOption(item) {
            this.hgkzOption = {};
            let that = this;
            this.hgkzOption = {
                chart: {
                    backgroundColor: null,
                    //zoomType: "",
                    style: {
                        fontSize: 20,
                        color: this.StyleColor
                    }
                },
                exporting:{
                    enabled:false
                },
                title: {
                    //text: '宏观控制图',
                    style: {
                        color: this.StyleColor
                    }
                },
                annotations: [{
                    labelOptions: {
                        backgroundColor: 'rgba(255,255,255,0)',
                        verticalAlign: 'top',
                        borderColor: 'ffffff',
                        y: 1,
                        style: {
                            color: this.StyleColor
                        }
                    },
                    draggable: '',
                    labels: [
                        {
                            point: {
                                xAxis: 0,
                                yAxis: 0,
                                x: 40,
                                y: 0.5
                            },
                            text: '欠注区',

                        },
                        {
                            point: {
                                xAxis: 0,
                                yAxis: 0,
                                x: 100,
                                y: 0.5
                            },
                            text: '正常区',
                            style: {
                                color: this.StyleColor
                            }
                        },
                        {
                            point: {
                                xAxis: 0,
                                yAxis: 0,
                                x: 160,
                                y: 0.5
                            },
                            text: '超注区',
                            style: {
                                color: this.StyleColor
                            }
                        },
                        {
                            point: {    
                                xAxis: 0,
                                yAxis: 0,
                                x: 96,
                                y: 1.1
                            },
                            text: '超压区',
                            style: {
                                color: this.StyleColor
                            }
                        }
                    ]
                }],
                tooltip: {
                    shared: true,  // 提示框是否共享
                    enabled: true, // 是否启用提示框， 默认启用
                    formatter: function (value) {
                       
                        // 提示框格式化字符串
                        var s = '日期:<b>' + this.point.z + '<br/>井名：<b>' + this.point.wellName+'</b><br/>配注完成率(%):<b>' + this.x + '</b><br/>井口无因次注入压力(MPa):<b>' + this.y + '</b>';
                        return s;
                    },
                },
                legend: {
                    enabled: false,
                },
                plotOptions: {
                    line: {
                        enableMouseTracking: false,
                        marker: {
                            enabled: false,
                        },
                        lineWidth: 2,

                    }, bubble: {
                        minSize: 6,
                        maxSize: 8
                    },
                    series:{
                        point:{
                            events:{
                                click:function(event){
                                    that.pointClickFun(event);
                                }
                            }
                        }
                    }
                },
                xAxis: {
                    min: 0,
                    max: 200,
                    lineColor: this.StyleColor,
                    lineWidth: 1,
                    tickInterval: 30,
                    gridLineWidth: 0,
                    minorGridLineWidth: 0,
                    title: {
                        text: '配注完成率(%)',
                        style: {
                            color: this.StyleColor
                        },
                        labels: {
                            style: {
                                color: this.StyleColor
                            }
                        }
                    },
                    labels: {
                        style: {
                            color: this.StyleColor
                        }
                    },
                    tickWidth: 1,
                    tickColor: this.StyleColor
                },
                yAxis: {
                    min: 0,
                    max: 1.5,
                    lineColor: this.StyleColor,
                    lineWidth: 1,
                    gridLineWidth: 0,
                    minorGridLineWidth: 0,
                    title: {
                        text: '井口无因次注入压力',
                        style: {
                            color: this.StyleColor
                        }
                    },
                    labels: {
                        style: {
                            color: this.StyleColor
                        }
                    },
                    tickWidth: 1,
                    tickInterval: 0.3,
                    tickColor: this.StyleColor
                },
                series: [{
                    type: 'line',
                    data: [[0, Number(this.addWaterForm.wellDimenspress)], [200, Number(this.addWaterForm.wellDimenspress)]]
                }, {
                    type: 'line',
                    data: [[Number(this.addWaterForm.wellLowerLimiRate), 0], [Number(this.addWaterForm.wellLowerLimiRate), Number(this.addWaterForm.wellDimenspress)]]
                }, {
                    type: 'line',
                    data: [[Number(this.addWaterForm.wellUpLimiRate), 0], [Number(this.addWaterForm.wellUpLimiRate),Number(this.addWaterForm.wellDimenspress)]]
                },
                {
                        name: '',
                        type: 'bubble',
                        data: [],
                        minSize: '6',
                        maxSize: '12',
                    },]
            }
            if(item != 1){
                if(item.length >0){
                    item.forEach(element => {
                        this.hgkzOption.series[3].data.push({x:element.allocInjRatio,y:element.injPressDimens,z:element.prodDate,wellName:element.wellName,wellId:element.wellId})
                    });
                }
            }
        },
        getcwzsOption(item) {
            this.cwzsOption = {
                chart: {
                    backgroundColor: null,
                    //zoomType: "",
                    style: {
                        fontSize: 20,
                        color: this.StyleColor
                    }
                },
                exporting:{
                    enabled:false
                },
                title: {
                    //text: '层位注水变化趋势图',
                    style: {
                        color: this.StyleColor
                    }
                },
                annotations: [{
                    labelOptions: {
                        backgroundColor: 'rgba(255,255,255,0)',
                        verticalAlign: 'top',
                        borderColor: 'ffffff',
                        y: 1,
                        style: {
                            color: this.StyleColor
                        }
                    },
                    draggable: '',
                    labels: [
                        {
                            point: {
                                xAxis: 0,
                                yAxis: 0,
                                x: 40,
                                y: 0.5
                            },
                            text: '欠注区',

                        },
                        {
                            point: {
                                xAxis: 0,
                                yAxis: 0,
                                x: 100,
                                y: 0.5
                            },
                            text: '正常区',
                            style: {
                                color: this.StyleColor
                            }
                        },
                        {
                            point: {
                                xAxis: 0,
                                yAxis: 0,
                                x: 160,
                                y: 0.5
                            },
                            text: '超注区',
                            style: {
                                color: this.StyleColor
                            }
                        },
                        {
                            point: {
                                xAxis: 0,
                                yAxis: 0,
                                x: 96,
                                y: 1.1
                            },
                            text: '超压区',
                            style: {
                                color: this.StyleColor
                            }
                        },
                    ]
                }],
                tooltip: {
                    shared: true,  // 提示框是否共享
                    enabled: true, // 是否启用提示框， 默认启用
                    formatter: function () {
                        // 提示框格式化字符串
                        var s = '日期:<b>' + this.point.z + '<br/>层名：<b>' + this.point.layerName+'</b><br/>配注完成率(%):<b>' + this.x + '</b><br/>井口无因次注入压力(MPa):<b>' + this.y + '</b>';
                        return s;
                    },
                },
                legend: {
                    enabled: false,
                },
                plotOptions: {
                    line: {
                        enableMouseTracking: false,
                        marker: {
                            enabled: false,
                        },
                        lineWidth: 2,

                    }, bubble: {
                        minSize: 6,
                        maxSize: 8
                    }
                },
                xAxis: {
                    min: 0,
                    max: 200,
                    lineColor: this.StyleColor,
                    lineWidth: 1,
                    tickInterval: 30,
                    gridLineWidth: 0,
                    minorGridLineWidth: 0,
                    title: {
                        text: '配注完成率(%)',
                        style: {
                            color: this.StyleColor
                        },
                        labels: {
                            style: {
                                color: this.StyleColor
                            }
                        }
                    },
                    labels: {
                        style: {
                            color: this.StyleColor
                        }
                    },
                    tickWidth: 1,
                    tickColor: this.StyleColor
                },
                yAxis: {
                    min: 0,
                    max: 1.5,
                    lineColor: this.StyleColor,
                    lineWidth: 1,
                    gridLineWidth: 0,
                    minorGridLineWidth: 0,
                    title: {
                        text: '井口无因次注入压力',
                        style: {
                            color: this.StyleColor
                        }
                    },
                    labels: {
                        style: {
                            color: this.StyleColor
                        }
                    },
                    tickWidth: 1,
                    tickInterval: 0.3,
                    tickColor: this.StyleColor
                },
                series: [
                    {
                        name: '',
                        type: 'bubble',
                        data: [
                           
                        ],
                        minSize: '6',
                        maxSize: '12',
                    },

                    {
                        type: 'line',
                        data: [[0, Number(this.addWaterForm.layerDimenspress)], [200, Number(this.addWaterForm.layerDimenspress)]]
                    },
                    {
                        type: 'line',
                        data: [[Number(this.addWaterForm.layerLowerLimiRate), 0], [Number(this.addWaterForm.layerLowerLimiRate), Number(this.addWaterForm.layerDimenspress)]]
                    },
                    {
                        type: 'line',
                        data: [[Number(this.addWaterForm.layerUpLimiRate), 0], [Number(this.addWaterForm.layerUpLimiRate), Number(this.addWaterForm.layerDimenspress)]]
                    },
                ]
            }
            if(item != 1){
                if(item.length > 0){
                    item.forEach(element => {
                       
                        this.cwzsOption.series[0].data.push({x:element.allocInjRatio,y:element.injPressDimens,z:element.prodDate,layerName:element.productionIntervalName})
                    });
                }
            }
        },
        async getHongGuanChartData(item) {
            let param;
            if(item == 1){
                param ={
                    ogfId:this.ogfId,
                    endDate: this.ProdDate.dateRange[1],
                    startDate: this.ProdDate.dateRange[0],
                   
                }
            }else if(item == 2){
                param ={
                    platformId:this.platformId,
                    endDate: this.ProdDate.dateRange[1],
                    startDate: this.ProdDate.dateRange[0],
                   
                }
            }else if(item == 3){
                param ={
                    wellId:this.wellId,
                    endDate: this.ProdDate.dateRange[1],
                    startDate: this.ProdDate.dateRange[0],
                    //layerType: this.ProdDate.radio,
                }   
            }
            //获取宏观控制图数据
            await InjectWaterMacroEvaluation.queryData(param).then( (response) =>{
               
                let result = response.data;
                
                if(result.code == 200){
                    
                    if(item == 3){
                        this.tableData = result.data;
                        for(let i=0;i<this.tableData.length;i++){
                            if(i == 0){
                                this.tableData[0].allocInjRatiochange = 0;
                            }else{
                                this.tableData[i].allocInjRatiochange = Math.abs(this.tableData[i].allocInjRatio - this.tableData[i - 1].allocInjRatio).toFixed(2)
                            }
                        }
                        this.getcwzsOption(this.tableData);
                    }else{
                        this.gethgkzOption(result.data);
                        if(result.data.length < 1){
                            this.getcwzsOption(1);
                            this.tableData = [];
                        }
                    }
                }else{
                    this.getcwzsOption(1);
                    this.tableData = [];
                }
            })
        },
        updateStyle(item) {

            if (item == 'dark') {
                this.StyleColor = 'white';
            } else {
                this.StyleColor = 'black';

            }
        },
        pointClickFun(event){
            this.wellId = event.point.wellId;
            
            this.getHongGuanChartData(3)
        }
    }
}
</script>
<style lang='less' scoped>


</style>
