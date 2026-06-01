<template>
  <div ref="cmTableWrap" class="cmTable-wrap">
    <div
      v-if="title"
      class="table-title area-title"
    >
      <span>
        {{ title }}
      </span>
      <span class="table-right">
        <slot name="titleTools" />
      </span>
    </div>
    <el-table
      :height="tableHeight"
      :data="tableList"
      v-bind="$attrs"
      style="width: 100%"
      :header-cell-style="{
        'text-align': 'center'
      }"
      v-on="$listeners"
    >
      <slot name="tableColumn" />
    </el-table>
  </div>
</template>
<script>
export default {
  name: "CmTable",
  props: {
    tableData: {
      type: Array,
      default: () => []
    },
    title: {
      type: String,
      default: ""
    },
    autoHeight: {
      type: Boolean,
      default: true
    },
    height: {
      type: Number,
      default: null
    }
  },
  data() {
    return {
      tableHeight: null
    };
  },
  computed: {
    tableList() {
      return this.tableData;
    }
  },
  mounted() {
    if (this.autoHeight) {
      this.tableHeight = this.title
        ? this.$refs.cmTableWrap.offsetHeight - 30
        : this.$refs.cmTableWrap.offsetHeight;
    } else {
      this.tableHeight = this.height;
    }
  },
  methods: {
    resize() {
      this.tableHeight = this.title
        ? this.$refs.cmTableWrap.offsetHeight - 30
        : this.$refs.cmTableWrap.offsetHeight;
    }
  }
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
    background: linear-gradient(
      to bottom,
      rgb(52, 201, 221),
      rgb(19, 112, 122)
    );
  }
}
</style>
