    <template>
            <el-form :model="form" :rules="rules" ref="dialogForm" label-width="100px" label-position="left">
                <el-row :gutter="20">
                    <el-col :span="12">
                        <el-form-item label="坐标轴标题" prop="title">
                            <el-input v-model="form.title" clearable placeholder="坐标轴显示的名称" style="width:240px;"></el-input>
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item label="坐标轴线颜色" prop="lineColor">
                            <el-color-picker v-model="form.lineColor"></el-color-picker>
                        </el-form-item>
                    </el-col>
                </el-row>

                <el-row :gutter="20">
                    <el-col :span="12">
                        <el-form-item label="坐标轴线位置" prop="tickPosition">
                            <el-radio-group v-model="form.tickPosition" style="width:240px;">
                                <el-radio-button label="outside">外部</el-radio-button>
                                <el-radio-button label="inside">内部</el-radio-button>
                            </el-radio-group>
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item label="轴刻度间隔值" prop="tickInterval">
                            <el-input-number v-model="form.tickInterval" type="number" controls-position="right" :min="0" />
                        </el-form-item>
                    </el-col>
                </el-row>
                
                <el-row v-if="form.type=='datetime'" :gutter="20">
                    <el-col :span="12">
                        <el-form-item label="坐标轴最小值" prop="min">
                            <el-date-picker v-model="form.min"  type="date"  placeholder="选择最小日期"> </el-date-picker>
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item label="坐标轴最大值" prop="max">
                            <el-date-picker v-model="form.max"  type="date"  placeholder="选择最大日期"> </el-date-picker>
                        </el-form-item>
                    </el-col>
                </el-row>

                <el-row v-else :gutter="20">
                    <el-col :span="12">
                        <el-form-item label="坐标轴最小值" prop="min">
                            <el-input-number v-model="form.min" type="number" clearable></el-input-number>
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item label="坐标轴最大值" prop="max">
                            <el-input-number v-model="form.max" type="number" clearable></el-input-number>
                        </el-form-item>
                    </el-col>
                </el-row>
                <el-row :gutter="20">
                    <el-col :span="12">
                        <el-form-item label="轴刻度线长度" prop="tickLength">
                            <el-input-number v-model="form.tickLength" type="number" controls-position="right" :min="1" />
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item label="轴刻度线宽度" prop="tickWidth">
                            <el-input-number v-model="form.tickWidth" type="number" controls-position="right" :min="0.5" :precision="1"/>
                        </el-form-item>
                    </el-col>
                </el-row>	
            </el-form>
    </template>

<script>

export default {
    props: {
    },
    data(){
        return {
            form: {
                type:'',
                title: null,
                lineColor: "",
                tickColor: "",
                tickPosition: "",
                tickInterval:null,
                tickLength:null,
                tickWidth:null,
                max:null,
                min:null
            }, 
            rules: [],
            loading: false
        }
    },
    mounted() {

    },
    methods: {
        //保存
        getData(){
            return {
                    title:{text:this.form.title},
                    lineColor:this.form.lineColor,
                    tickColor:this.form.tickColor,
                    tickPosition:this.form.tickPosition,
                    tickInterval:this.form.tickInterval,
                    tickLength:this.form.tickLength,
                    tickWidth:this.form.tickWidth,
                    max:this.form.type=="datetime"?this.form.max.getTime():this.form.max,
                    min:this.form.type=="datetime"?this.form.min.getTime():this.form.min,
                };
        },
        //表单注入数据
        setData(data){
            this.form ={
                type:data.options.type,
                title:data.options.title.text,
                lineColor:data.options.lineColor,
                tickColor:data.options.tickColor,
                tickPosition:data.options.tickPosition,
                tickInterval:data.tickInterval==undefined?null:data.tickInterval,
                tickLength:data.options.tickLength==undefined?null:data.options.tickLength,
                tickWidth:data.options.tickWidth==undefined?null:data.options.tickWidth,
                max:data.max,
                min:data.min
            };

            if(data.options.type=='datetime'){
                this.form.tickInterval=null;
                this.form.max=new Date(data.max);
                this.form.min=new Date(data.min);
            }            
        }
    }
}
</script>

<style scoped>
h2 {font-size: 17px;color: #3c4a54;padding:0 0 30px 0;}
.apilist {border-left: 1px solid #eee;}

[data-theme="dark"] h2 {color: #fff;}
[data-theme="dark"] .apilist {border-color: #434343;}
</style>
