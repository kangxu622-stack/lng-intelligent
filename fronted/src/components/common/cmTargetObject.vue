<!--目标对象组件-->
<template>
  <div class="target-object-container">
    <el-cascader
      ref="targetObject"
      style="width: 100%"
      v-model="ruleForm.wellId"
      :options="treeData"
      :props="defaultProps"
      :show-all-levels="false"
      v-bind="$attrs"
      v-on="$listeners"
      @visible-change="handleVisibleChange"
    >
      <div slot-scope="{ node, data }">
        <i v-if="data.wellType == 1" class="well-icon"></i>
        <i v-else-if="data.wellType == 2" class="water-icon"></i>
        <span>{{ node.label }}</span>
      </div>
    </el-cascader>
  </div>
</template>

<script>
export default {
  components: {},
  props: {
    dataList: {
      type: Array,
      default: () => {
        return [];
      },
    },
    // 树形控件默认属性
    defaultProps: {
      type: Object,
      default: () => {
        return {
          children: "children",
          label: "name",
          value: "id",
          checkStrictly: true,
          emitPath: false,
        };
      },
    },
  },
  data() {
    return {
      loading: false,
      ruleForm: {
        wellId: "3FC9A818F5BC43B88270DB80BBB3018F",
      },
      treeData: [], // 目标对象数据
    };
  },
  watch: {
    dataList: {
      handler(newVal) {
        this.treeData = newVal;
        if (this.$store.state.zcfalt.planInitBlockId) {
          this.ruleForm.wellId = this.$store.state.zcfalt.planInitBlockId;
        } else {
          // this.ruleForm.wellId = "3FC9A818F5BC43B88270DB80BBB3018F";
        }
        this.$nextTick(() => {
          // 初始化秦皇岛32-6
          this.handleVisibleChange(false);
        });
      },
      deep: true,
      immediate: true,
    },
    // 用来设置目标对象值回显
    "$store.state.zcfalt.targetInitChange": {
      handler(val) {
        if (val) {
          this.ruleForm.wellId = val.id;
        }
      },
      deep: true,
    },
  },
  methods: {
    //加载首层树数据
    async loadTreeData() {
      this.loading = true;
      await getDeptTreeDatas({
        type: this.displayStatus,
      }).then((response) => {
        let result = response.data;
        if (result.code !== 200) {
          this.$message(result.msg);
          return;
        } else {
          this.treeData = result.data;
          this.loading = false;
        }
      });
    },
    // 树节点弹窗消失时触发
    handleVisibleChange(val) {
      if (!val) {
        let treeData = this.$refs.targetObject.getCheckedNodes();
        let saveTreeData = treeData[0] ? treeData[0].data : {};
        this.$emit("treeData", saveTreeData);
        if (saveTreeData.id) {
          let level = 0;
          if (saveTreeData.entityType === 6) {
            level = 3;
          } else if (saveTreeData.entityType === 10) {
            level = 4;
          }
          let targetIdObj = {
            level: level,
            value: treeData[0]?.value,
            ...saveTreeData,
          };
          this.$emit("targetId", targetIdObj);
          // this.$store.commit("zcfalt/setTargetWellIdWatch", ""); // 点击时清空回显初始值，否者会出问题
        }
      }
    },
  },
};
</script>
<style lang="scss" scoped>
.target-object-container {
  width: 100%;
}
.well-icon {
  width: 12px;
  height: 12px;
  border: 1px solid #999;
  background: red;
  border-radius: 6px;
  margin: 0px 10px;
  display: inline-block;
}
.water-icon {
  width: 12px;
  height: 12px;
  border: 1px solid #999;
  background: blue;
  border-radius: 6px;
  margin: 0px 10px;
  display: inline-block;
}
</style>
