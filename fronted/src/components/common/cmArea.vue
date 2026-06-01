<template>
  <div
    class="temp--area-container wh100"
    :class="{ 'light-bg-logo': $store.state.setting.mode !== 'dark', border: border }"
  >
    <div v-if="isTitle" class="temp-area-title primary-title-bg">
      <template v-if="title">
        <span>{{ title }}</span>
      </template>
      <template v-else>
        <slot name="title" />
      </template>
    </div>
    <el-scrollbar class="temp-area-main">
      <slot />
    </el-scrollbar>
  </div>
</template>

<script>
export default {
  props: {
    title: {
      type: String,
      default: ""
    },
    isTitle: {
      type: Boolean,
      default: false
    }
  },
  data() {
    return {
      fullScreenStyle: null,
      isFullScreen: false
    };
  },
  computed: {
    border() {
      return Object.prototype.hasOwnProperty.call(this.$attrs, "border");
    }
  },
  methods: {}
};
</script>

<style lang="scss" scoped>
.temp--area-container {
  position: relative;
  display: flex;
  flex-direction: column;
  .temp-area-title {
    padding: 5px 10px;
    box-sizing: border-box;
    display: flex;
    font-size: 14px;
    align-items: center;
    position: relative;
    justify-content: space-between;
    .full-screen {
      cursor: pointer;
      color: var(--td-text-color-primary);
    }
  }
  .temp-area-main {
    flex: 1;
    padding: 10px;
    box-sizing: border-box;
  }
}
.dark {
  .temp-area-title {
    background: linear-gradient(270deg, rgba(0, 202, 255, 0.68) 0%, rgba(0, 150, 255, 0) 100%);
    border-bottom: 1px solid;
    border-image: linear-gradient(180deg, rgba(116, 190, 243, 0), rgba(0, 180, 255, 0.6)) 1 1;
    color: #24deff;
  }
  .border {
    border: 1px solid #ddd;
    border-image: linear-gradient(180deg, #2e5b7c, #01aaf2) 3 3;
    box-shadow: unset;
    background: var(--logo-bg) no-repeat top right,
      linear-gradient(360deg, rgba(0, 68, 115, 0.64) 0%, rgba(0, 72, 122, 0.16) 100%);
  }
}
.light {
  .temp--area-container {
    background: #fff !important;
    box-shadow: 0 0 10px 0 rgba(144, 147, 153, 0.3) !important;
    border: 1px solid #fff !important;
    color: #333 !important;
  }
  .temp-area-title {
    border-bottom: 1px solid #e7e1e1;
    color: var(--light-blue-color);
  }
  .primary-title-bg {
    background: var(--light-blue-color);
    color: #fff;
    border-bottom: none;
    i {
      color: #fff !important;
    }
  }
  .border {
    border: 1px solid var(--border-color-2) !important;
  }
}
</style>
