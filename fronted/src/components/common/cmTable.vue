<template>
  <div ref="cmTableWrap" class="cmTable-wrap flex-column">
    <div :style="{ height: showPagination ? 'calc(100% - 40px)' : '100%' }">
      <div v-if="title" class="table-title area-title">
        <span>
          {{ title }}
        </span>
        <span class="table-right">
          <slot name="titleTools" />
        </span>
      </div>
      <el-form ref="ruleForm" :model="ruleForm" label-width="0" :validate-on-rule-change="false" style="height: 100%">
        <el-table
          ref="cmTable"
          :key="tableKey"
          :height="tableHeight"
          :max-height="maxHeight"
          :data="ruleForm.tableData"
          style="width: 100%"
          :header-cell-style="{
            'text-align': 'center',
          }"
          v-bind="$attrs"
          v-on="$listeners"
          @cell-dblclick="doubleClick"
        >
          <template v-if="!isSlot">
            <el-table-column v-if="showSelection" type="selection" width="40" />
            <el-table-column v-if="showIndex" type="index" />
            <el-table-column v-for="(item, index) in tableColumns" :key="index" :prop="item.prop" :label="item.label" :width="item.width" :align="item.align">
              <template slot-scope="scope">
                <template v-if="item.isEdit">
                  <el-form-item
                    v-if="scope.row[scope.column.property + 'Show']"
                    :prop="`tableData.${scope.$index}.${scope.column.property}`"
                    :rules="
                      item.isRules ? item.rules || [{ required: true, message: ' ', trigger: ['focus', 'change'] }] : []
                    "
                    style="margin-bottom: 0; position: relative; top: 50%; transform: translateY(-50%)"
                  >
                    <el-input
                      v-if="item.type === 'input'"
                      v-model="scope.row[scope.column.property]"
                      v-focus
                      clearable
                      @keyup.enter.native="onBlur(scope.row, scope.column, item)"
                      @blur="onBlur(scope.row, scope.column, item)"
                    />
                    <el-select
                      v-if="item.type === 'select'"
                      v-model="scope.row[scope.column.property]"
                      v-focus
                      style="width: 100%"
                      @blur="onBlur(scope.row, scope.column, item)"
                    >
                      <el-option
                        v-for="val in item?.config.options"
                        :key="val.value"
                        :label="val.label"
                        :value="val.value"
                      />
                    </el-select>
                    <el-date-picker
                      v-if="item.type === 'date'"
                      v-model="scope.row[scope.column.property]"
                      v-focus
                      style="width: 100%"
                      type="date"
                      range-separator="至"
                      start-placeholder="开始月份"
                      end-placeholder="结束月份"
                      format="yyyy-mm-dd"
                      value-format="yyyy-mm-dd"
                      @blur="onBlur(scope.row, scope.column, item)"
                    />
                  </el-form-item>
                  <span v-else>{{ scope.row[item.prop] }}</span>
                </template>
                <template v-else>{{ scope.row[item.prop] }}</template>
              </template>
            </el-table-column>
          </template>
          <slot v-else name="tableColumn" />
        </el-table>
      </el-form>
    </div>
    <div class="flex-end align-center" v-if="showPagination" style="padding-top: 10px; box-sizing: border-box">
      <el-pagination
        class="custom--pagination"
        @size-change="handleSizeChange"
        @current-change="handleCurrentChange"
        :current-page="currentPage"
        :page-sizes="pageSizes"
        :page-size="pageSize"
        :layout="layout"
        :total="total"
      >
      </el-pagination>
    </div>
  </div>
</template>
<script>
export default {
  name: "CmTable",
  directives: {
    focus: {
      // 指令的定义
      inserted: function (el) {
        el.querySelector("input").focus();
      },
    },
  },
  props: {
    // 表格数据
    tableData: {
      type: Array,
      default: () => [
        {
          date: "2016-05-02",
          name: "",
          address: "",
        },
      ],
    },
    // 表格列
    columns: {
      type: Array,
      default: () => [
        {
          prop: "date",
          label: "日期",
          width: "180",
          type: "date",
          isEdit: true,
          isRules: true,
        },
        {
          prop: "name",
          label: "姓名",
          width: "180",
          type: "input",
          isEdit: true,
          isRules: true,
        },
        {
          prop: "address",
          label: "地址",
          type: "select",
          isEdit: true,
          isRules: true,
          config: {
            options: [
              {
                value: "选项1",
                label: "选项1",
              },
              {
                value: "选项2",
                label: "选项2",
              },
            ],
          },
        },
      ],
    },
    // 是否显示多选框
    showSelection: {
      type: Boolean,
      default: false,
    },
    // 是否显示序号
    showIndex: {
      type: Boolean,
      default: false,
    },
    // 是否显示表头
    title: {
      type: String,
      default: "",
    },
    // 是否自适应高度
    autoHeight: {
      type: Boolean,
      default: true,
    },
    // 表格高度
    height: {
      type: Number,
      default: null,
    },
    // 表格最大高度
    maxHeight: {
      type: Number,
      default: null,
    },
    // 表格列是否插槽
    isSlot: {
      type: Boolean,
      default: true,
    },
    layout: {
      type: String,
      default: "total, sizes, prev, pager, next, jumper",
    },
    // 表格数据总数
    total: {
      type: Number,
      default: 0,
    },
    pageSize: {
      type: Number,
      default: 10,
    },
    pageSizes: {
      type: Array,
      default: () => [10, 50, 100, 200],
    },
    pageNum: {
      type: Number,
      default: 1,
    },
    // 是否显示分页
    showPagination: {
      type: Boolean,
      default: false,
    },
  },
  data() {
    return {
      // currentPage: 1,
      tableHeight: null,
      tableKey: null,
      ruleForm: {
        tableData: [],
      },
      tableColumns: [],
    };
  },
  computed:{
    currentPage () {
      return this.pageNum;
    }
  },
  watch: {
    tableData: {
      handler(val) {
        this.ruleForm.tableData = val;
      },
      deep: true,
      immediate: true,
    },
    columns: {
      handler(val) {
        this.tableColumns = val;
      },
      deep: true,
      immediate: true,
    },
  },
  mounted() {
    this.$nextTick(() => {
      this.setTableHeight();
    });
    const resizeObserver = new ResizeObserver((entries) => {
      // 当DOM元素的宽高发生变化时执行回调函数
      this.setTableHeight();
    });
    resizeObserver.observe(this.$refs.cmTableWrap);
    // 组件销毁时暂停观察
    this.$once("hook:beforeDestroy", () => {
      resizeObserver.disconnect();
    });
  },
  methods: {
    // 动态设置表格高度
    setTableHeight() {
      if (this.autoHeight) {
        this.tableHeight = this.title
          ? this.$refs.cmTableWrap.offsetHeight - 31 - (this.showPagination ? 40 : 0)
          : this.$refs.cmTableWrap.offsetHeight - 1 - (this.showPagination ? 40 : 0);
      } else {
        this.tableHeight = this.height;
      }
    },
    // 双击单元格触发事件
    doubleClick(row, column) {
      if (!row[`${column.property}Show`]) {
        row[`${column.property}Show`] = true;
        this.updateTable();
      }
      this.$nextTick(() => {
        this.$refs.ruleForm.validate();
      });
    },
    // 输入框失焦事件
    onBlur(row, column, item) {
      if (item.type === "select") {
        // select的blur事件大于change事件，导致值没法回显，所以增加定时器处理
        setTimeout(() => {
          row[`${column.property}Show`] = false;
          this.updateTable();
          this.$emit("onUpdate", row);
        }, 50);
      } else {
        row[`${column.property}Show`] = false;
        this.updateTable();
        this.$emit("onUpdate", row);
      }
    },
    // 更新表格
    updateTable() {
      this.tableKey = Math.random();
    },
    // size变化
    handleSizeChange(val) {
      this.$emit("onSize", val);
    },
    // current变化
    handleCurrentChange(val) {
      this.$emit("onCurrent", val);
    },
    // 重置
    resize() {
      this.tableHeight = this.title
        ? this.$refs.cmTableWrap.offsetHeight - 31 - (this.showPagination ? 40 : 0)
        : this.$refs.cmTableWrap.offsetHeight + 1 - (this.showPagination ? 40 : 0);
    },
  },
};
</script>
<style lang="scss" scoped>
.cmTable-wrap {
  height: 100%;
  width: 100%;
  overflow: hidden;
  .table-title {
    height: 30px;
    .table-right {
      margin-left: auto;
    }
  }
}
.area-title {
  color: var(--td-text-color-primary);
  display: flex;
  font-size: 14px;
  align-items: center;
  position: relative;
  &::before {
    content: "";
    position: relative;
    top: 1px;
    height: 15px;
    width: 3px;
    margin-right: 5px;
    background: linear-gradient(to bottom, rgb(52, 201, 221), rgb(19, 112, 122));
  }
}
</style>
