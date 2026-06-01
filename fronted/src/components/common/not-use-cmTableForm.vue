<template>
  <el-form ref="ruleForm" :model="ruleForm" label-width="0" :validate-on-rule-change="false">
    <el-table :key="key" :data="ruleForm.tableData" border style="width: 100%" @cell-dblclick="doubleClick">
      <template v-if="!isSlot">
        <el-table-column v-for="(item, index) in tableColumns" :key="index" :prop="item.prop" :label="item.label">
          <template slot-scope="scope">
            <template v-if="item.isEdit">
              <el-form-item
                v-if="scope.row[scope.column.property + 'Show']"
                :prop="`tableData.${scope.$index}.${scope.column.property}`"
                :rules="{ required: true, message: ' ', trigger: ['focus', 'change'] }"
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
      <slot v-else />
    </el-table>
  </el-form>
</template>

<script>
export default {
  name: "cmTableForm",
  directives: {
    focus: {
      // 指令的定义
      inserted: function (el) {
        el.querySelector("input").focus();
      }
    }
  },
  props: {
    // 表格列是否插槽
    isSlot: {
      type: Boolean,
      default: false
    },
    // 表格数据
    tableData: {
      type: Array,
      default: () => [
        {
          date: "2016-05-02",
          name: "",
          address: ""
        },
        {
          date: "2016-05-04",
          name: "王小虎",
          address: "上海市普陀区金沙江路 1517 弄"
        },
        {
          date: "2016-05-01",
          name: "王小虎",
          address: "上海市普陀区金沙江路 1519 弄"
        },
        {
          date: "2016-05-03",
          name: "王小虎",
          address: "上海市普陀区金沙江路 1516 弄"
        }
      ]
    },
    // 表格列
    columns: {
      type: Array,
      default: () => [
        {
          prop: "date",
          label: "日期",
          width: "180",
          isEdit: true,
          type: "date"
        },
        {
          prop: "name",
          label: "姓名",
          width: "180",
          isEdit: true,
          type: "input"
        },
        {
          prop: "address",
          label: "地址",
          isEdit: true,
          type: "select",
          config: {
            options: [
              {
                value: "选项1",
                label: "选项1"
              },
              {
                value: "选项2",
                label: "选项2"
              }
            ]
          }
        }
      ]
    }
  },
  data() {
    return {
      key: Math.random(),
      ruleForm: {
        tableData: []
      },
      tableColumns: []
    };
  },
  watch: {
    tableData: {
      handler(val) {
        this.ruleForm.tableData = val;
      },
      deep: true,
      immediate: true
    },
    columns: {
      handler(val) {
        this.tableColumns = val;
      },
      deep: true,
      immediate: true
    }
  },
  methods: {
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
      this.key = Math.random();
    }
  }
};
</script>

<style lang="scss" scoped></style>
