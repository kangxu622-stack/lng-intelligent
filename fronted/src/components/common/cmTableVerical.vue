<template>
  <div class="cm-table-verical" id="cmTableVerical">
    <el-scrollbar class="wrap" always="true" v-if="showScroll">
      <ul v-if="isChecked" class="table-name-bg" style="position: absolute; left: 0; top: 0; bottom: 0; width: 37px">
        <li v-for="item in tableData" :key="item.key">
          <el-checkbox v-if="item.isChecked !== false" v-model="item.checked" @change="handleCheckChange"></el-checkbox>
        </li>
      </ul>
      <ul class="table-name table-name-bg" id="tableName" :style="{ left: isChecked ? '37px' : '0' }">
        <li v-for="item in tableData" v-show="item.isShow" :key="item.key" :style="{ width: `${keyColumnWidth}px` }">
          {{ item.name }}
        </li>
      </ul>
      <div :style="{ paddingLeft: `${pl}px`, display: 'flex' }" :key="tableKey">
        <ul class="table-column" v-for="(item, index) in column" :key="`item.key${index}`" style="flex: 1">
          <li
            v-for="(val, i) in tableData"
            v-show="val.isShow"
            :style="{ minWidth: '100px', textAlign: i === 0 ? 'center' : val.align ? val.align : 'center' }"
            @dblclick="handleDBclick(item, val, index)"
          >
            <template v-if="val.isEdit !== false">
              <el-input
                :style="{ minWidth: `100px` }"
                v-if="val[val.key + index + 'Show']"
                v-model="item[val.key]"
                v-focus
                clearable
                minlength="1"
                @keyup.enter.native="onBlur(item, val, index)"
                @blur="onBlur(item, val, index)"
              />
              <span v-else>{{ item[val.key] }}</span>
            </template>
            <template v-else>
              <span>{{ item[val.key] }}</span>
            </template>
          </li>
        </ul>
      </div>
    </el-scrollbar>
  </div>
</template>

<script>
export default {
  components: {},
  directives: {
    focus: {
      // 指令的定义
      inserted: function (el) {
        el.querySelector("input").focus();
      },
    },
  },
  props: {
    tableData: {
      type: Array,
      default: () => [],
    },
    column: {
      type: Array,
      default: () => [],
    },
    isChecked: {
      type: Boolean,
      default: false,
    },
    keyColumnWidth: {
      type: Number,
      default: 150,
    },
  },
  data() {
    return {
      pl: 0,
      tableKey: null,
      showScroll: true,
    };
  },
  watch: {
    column() {
      this.showScroll = false;
      this.$nextTick(() => {
        this.showScroll = true;
      });
    },
  },
  created() {},
  mounted() {
    this.initStyle();
  },
  methods: {
    // 初始化样式
    initStyle() {
      this.pl = this.isChecked ? this.keyColumnWidth + 37 : this.keyColumnWidth;
    },
    // 双击单元格触发事件
    handleDBclick(row, column, index) {
      let isVal = true;
      this.tableData.forEach((e) => {
        this.column.forEach((f) => {
          if (!String(f[e.key])) {
            isVal = false;
          }
        });
      });
      if (isVal) {
        if (!column[column.key + index + "Show"]) {
          column[column.key + index + "Show"] = true;
          this.updateTable();
        }
      }
      // this.$nextTick(() => {
      //   this.$refs.ruleForm.validate();
      // });
    },
    // 输入框失焦事件
    onBlur(row, column, index) {
      if (!row[column.key]) {
        return;
      }
      column[column.key + index + "Show"] = false;
      this.updateTable();
      this.$emit("onUpdate", this.tableData, this.column);
    },
    // 选中切换
    handleCheckChange() {
      this.$emit("onUpdate", this.tableData, this.column);
    },
    // 更新表格
    updateTable() {
      this.tableKey = Math.random();
    },
  },
  // beforeDestroy() {
  //   this.$emit("onUpdate", this.tableData, this.column);
  // },
};
</script>
<style lang="scss" scoped>
.cm-table-verical {
  white-space: nowrap;
  border-left: 1px solid var(--table-border-color);
  border-right: 1px solid var(--table-border-color);
  position: relative;
  .table-name {
    position: absolute;
    top: 0;
    left: 0;
    bottom: 0;
    z-index: 1;
  }
  ul {
    display: inline-block;
    border-bottom: 1px solid var(--table-border-color);
    border-right: 1px solid var(--table-border-color);
    li {
      height: 40px;
      line-height: 40px;
      border: 1px solid var(--table-border-color);
      padding: 0 10px;
      box-sizing: border-box;
      border-bottom: 0;
      border-right: 0;
      border-left: 0;
    }
  }
}
:root[theme-mode="dark"] li {
  color: var(--white-color);
}
</style>
