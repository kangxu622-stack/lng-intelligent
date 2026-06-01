<template>
    
            <el-container style="height: 100%;">
                <el-main style="padding:0px 0px 5px 0px;height: calc(100% - 120px);">
                    <div style="display:flex;height: 100%;">
                        
                        <div class="rightBox" style="height: 100%;">
                            <pagePanelNew style="height: calc(100%);margin:0px;" >
                                <div class="headerBox">
                                    <el-input v-model="plyldlmd" style="width:240px" placeholder="请输入破裂压力当量密度(g/cm³)"/>
                                    <el-input v-model="aqxs" style="width:150px;margin:0px 20px;" placeholder="请输入安全系数"/>
                                    <el-button class="el-button--primary" @click="computeFun">计算</el-button>
                                    <el-button class="el-button--primary" @click="saveFun">保存</el-button>
                                    <el-button class="el-button--primary" icon="el-icon-download" @click="exportFun">下载</el-button>
                                    <el-button class="el-button--primary" @click="goBack">返回</el-button>
                                </div>
                                <el-table ref="table" id="ExportTables" v-loading="loading" height="calc(100% - 40px)" :data="listData" :border="true">
                                    <el-table-column type="index" width="50" label="序号" align="center"/>						
                                    <el-table-column label="井号" header-align="center" align="left" prop="wellName" min-width="120">
                                        
                                    </el-table-column>
                                    <el-table-column label="层段" header-align="center" show-overflow-tooltip="true" align="left" prop="layerName" min-width="100">
                                            
                                    </el-table-column>
                                    <el-table-column label="对应射孔段顶深(m)" header-align="center" show-overflow-tooltip="true" align="left" prop="topDepth"  min-width="140">
                                       
                                    </el-table-column>
                                    <el-table-column label="安全系数" header-align="center" align="left" prop="safeCoefficient" width="100">
                                        
                                    </el-table-column>
                                    <el-table-column label="破裂压力当量密度(g/cm³)" header-align="center" show-overflow-tooltip="true" align="left" prop="equDensity" min-width="160">
                                            
                                    </el-table-column>
                                    <el-table-column label="层段顶部破裂压力(MPa)" header-align="center" show-overflow-tooltip="true" align="left" prop="layersFracturePress" min-width="160">
                                            
                                    </el-table-column>
                                </el-table>
                            </pagePanelNew>	
                        </div>
                    </div>
                </el-main>
                <el-footer style="height:120px;padding:0px;">
                    <pagePanelNew style="height: calc(100%);margin:0px;" class="g-w100">
                        <div style="font-size:16px;">帮助说明</div>
                        <p class="PclassName">通过设置破裂压力当量密度计算层段顶部破裂压力。</p>
                        <p class="PclassName">通过设置安全系数为计算最大安全井底注入压力提供设计依据。</p>
                    </pagePanelNew>	    
                </el-footer>
            </el-container>
        
</template>

<script>
// import treeSelectionCustom from "@/pages/zclt/components/treeCheckbox.vue";
import { qjjk } from '@/api/znzclt/zclt/productionDynamics.js';
export default {
    name:'破裂压力计算设置',
    data(){
        return{
            listData:[],
            plyldlmd:null,//破裂压力当量密度
            aqxs:null,//安全系数
           
        }
    },
    components:{
        // treeSelectionCustom
    },
    
    methods:{
        //树选择井层节点
        treeCheckNodeFun(Arr){
            if(Arr.length < 1){
                this.listData = [];
            }else{
                this.getTableListFun(Arr)
            }
            
        },
        //计算
        async computeFun(){
            let param = this.listData;
            if(param.length < 0){
                return
            }else{
                param.forEach(element => {
                    element.equDensity = this.plyldlmd;
                    element.safeCoefficient = this.aqxs;
                });
            }
            await qjjk.CalculateRupturePressure(param).then( (response) =>{ 
                let result = response.data;
                if(result.code == 200){ 
                    this.listData = result.data;
                }else{
                    this.$message.error('查询失败！')
                }
                

            })

        },
        async saveFun(){
            await qjjk.SaveCalculationResultsRupturePressure(this.listData).then( (response) =>{ 
                let result = response.data;
                if(result.code == 200){ 
                    this.$message.success('保存成功！')
                }else{
                    this.$message.error('保存失败！')
                }
                

            })
        },
        //查询列表接口
        async getTableListFun(Arr){
            let paramArr = [];
            Arr.forEach( item =>{
                let Obj = {
                    layerId: "", 
                    layerName: "",	
                    wellId: "", 
                    wellName: ""	
                }
                if(item.entityType == 10){
                    Obj.wellId = item.id;
                    Obj.wellName = item.name;
                    paramArr.push(Obj);
                }else if(item.entityType == 11){
                    Obj.wellId = item.parentId;
                    Obj.wellName = item.wellName;
                    Obj.layerName = item.name;
                    Obj.layerId = item.id;
                    paramArr.push(Obj);
                }
            })
            await qjjk.listCalculationResultsRupturePressure(paramArr).then( (response) =>{ 
                let result = response.data;
              
                if(result.code == 200){ 
                    this.listData = result.data;
                    
                }else{
                    this.$message.error('查询失败！')
                }
                

            })
            
        },
        exportFun(){
            //生产表格对象
            let wb = this.$XLSX.utils.table_to_book(document.querySelector("#ExportTables"));
            //获取二进制字符串作为输出
            let wbout = this.$XLSX.write(wb, {bookType: "xlsx",bookSST: true,type: "array"});
            try{
                //导出文件
                this.$FileSaver.saveAs(new Blob([wbout], { type: "application/octet-stream" }),"破裂压力计算结果.xlsx");

            } catch(e) {
                console.log(e,wbout);

            }
            return wbout;
        },
        goBack(){
            this.$emit('goBack',false)
        },
        
    }

}
</script>

<style scoped>
.PclassName{padding-left:20px;}
.leftBox,.rightBox{height:calc(100vh - 318px)}
.leftBox{padding-right: 5px;width:20%;}
.rightBox{width:100%;}
.headerBox{height:40px;line-height: 32px;}
</style>