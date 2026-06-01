<template>
  <el-dialog
    :append-to-body="true"
    :close-on-click-modal="false"
    :close-on-press-escape="false"
    :before-close="handleClose"
    :custom-class="`temp--custom-dialog ${$store.state.setting.mode}`"
    :visible.sync="visible"
    v-bind="$attrs"
    v-on="$listeners"
  >
    <slot />
    <span v-if="isFooter" slot="footer">
      <slot name="footerLeft"></slot>
      <slot name="footerMain">
        <el-button @click="handleClose">{{ cancelText }}</el-button>
        <el-button type="primary" @click="handleConfirm">{{ confirmText }}</el-button>
      </slot>
      <slot name="footerRight"></slot>
    </span>
  </el-dialog>
</template>

<script>
export default {
  name: "CustomDialog",
  props: {
    visible: {
      type: Boolean,
      default: true
    },
    isFooter: {
      type: Boolean,
      default: true
    },
    // isConfirmBtn: {
    //   type: Boolean,
    //   default: true
    // },
    // isCancelBtn: {
    //   type: Boolean,
    //   default: true
    // },
    confirmText: {
      type: String,
      default: "确 定"
    },
    cancelText: {
      type: String,
      default: "取 消"
    }
  },
  computed: {
    // isConfirmBtn() {
    //   return this.isFooter && this.isConfirmBtn;
    // },
    // isCancelBtn() {
    //   return this.isFooter && this.isCancelBtn;
    // }
  },
  data() {
    return {
      dialogVisible: true
    };
  },
  methods: {
    handleClose() {
      this.$emit("onClose", false);
    },
    handleConfirm() {
      this.$emit("onConfirm", false);
    }
  }
};
</script>

<style lang="scss">
:root[theme-mode="dark"] {
  .temp--custom-dialog {
    .el-dialog__body {
      margin: 20px 20px 10px 20px !important;
      padding: 0;
    }
    .el-dialog__body .el-table--border .el-table__cell {
      border-right: 1px solid var(--opacity-blue-bg) !important;
    }
    .el-button + .el-button {
      margin-left: 10px !important;
    }
  }
}
.temp--custom-dialog {
  .el-dialog__body {
    margin: 20px 20px 10px 20px !important;
    padding: 0;
  }
}
</style>
