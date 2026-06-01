<template>
  <div class="temp--main-container">
    <slot name="header" />
    <div class="custom--table-wrap table-wrap">
      <slot />
    </div>
    <div v-if="isFooter" class="temp--main-pagination">
      <slot name="footer">共【109】条</slot>
    </div>
  </div>
</template>

<script>
export default {
  props: {
    isFooter: {
      type: Boolean,
      default: false
    }
  }
};
</script>

<style lang="scss" scoped>
.temp--main-container {
  width: 100%;
  height: 100%;
  display: flex;
  flex: 1;
  flex-direction: column;
  border: 1px solid #ddd;
  border-image: linear-gradient(180deg, #2e5b7c, #01aaf2) 3 3;
  box-shadow: unset;
  background: var(--logo-bg) no-repeat top right,
    linear-gradient(
      360deg,
      rgba(0, 68, 115, 0.64) 0%,
      rgba(0, 72, 122, 0.16) 100%
    );
  // background: #13263a;
  overflow: hidden;
  .table-wrap {
    flex: 1;
    padding: 10px;
    box-sizing: border-box;
    overflow: hidden;
  }
  .temp--main-pagination {
    padding: 10px;
    box-sizing: border-box;
    display: flex;
    align-items: center;
    color: #fff;
    font-size: 12px;
    box-shadow: 0 -2px 8px rgba(0, 0, 0, 0.2);
  }
}
.light {
  .temp--main-container {
    background: #fff !important;
    box-shadow: 0 0 10px 0 rgba(144, 147, 153, 0.3);
    border: 1px solid #fff;
  }
  .temp--main-pagination {
    color: #606266;
  }
}
</style>
