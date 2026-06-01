<template>
  <div
    class="temp--aside-container"
    :class="{ menuColor: $store.state.setting.mode !== 'dark' && !isCollapse }"
    :style="{ width: isExtend ? sideWidth : isCollapse ? '300px' : '30px' }"
  >
    <div v-show="isCollapse" style="display: flex;flex-direction: column;overflow: hidden;height: 100%;">
      <div class="temp-aside-title area-title">
        <span v-if="title">{{ title }}</span>
        <span v-else><slot name="title">目标筛选配置</slot></span>
        <span class="temp-aside-collapse is-collapse-icon">
          <template v-if="isExtend">
            <i v-if="!isExtendStatus" class="el-icon-s-fold" @click="handleCollapse('lf')" style="margin-right: 0px" />
          </template>
          <i v-else class="el-icon-s-fold" @click="handleCollapse" />
        </span>
      </div>
      <el-scrollbar style="flex: 1">
        <div class="temp-aside-main">
          <slot name="main" />
        </div>
      </el-scrollbar>
      <div v-if="isFooter" class="temp-aside-footer">
        <slot name="footer"> 选择调控目标:在指定油田区块范围内，依据配产、配注完成率筛选需要调控的目标油水井 </slot>
      </div>
    </div>
    <div v-show="!isCollapse" class="temp-aside-off" @click="handleCollapse('rrf')">
      <!-- <i class="el-icon-s-unfold" style="font-size: 20px;font-weight: 100;"/> -->
      <div class="temp-aside-off-item">
        <!-- <img v-if="$store.state.setting.mode === 'dark'" src="@/assets/treeSelectDark2.png" style="width: 16px; margin-bottom: 7px">
        <img v-else src="@/assets/treeSelectWhite2.png" style="width: 16px; margin-bottom: 7px"> -->
        <span class="temp-nav-text">{{ title }}</span>
      </div>
    </div>
    <template v-if="isExtend && sideWidth !== '30px'">
      <span v-if="!isExtendStatus" @click="handleCollapse('rf')" class="icon-collapse">
        <i class="el-icon-caret-right" />
      </span>
      <span v-else class="icon-collapse" @click="handleCollapse('llf')">
        <i class="el-icon-caret-left" />
      </span>
    </template>
  </div>
</template>

<script>
export default {
  props: {
    title: {
      type: String,
      default: "目标筛选配置"
    },
    // 是否可以展开容器一半
    isExtend: {
      type: Boolean,
      default: false
    },
    // 是否显示底部
    isFooter: {
      type: Boolean,
      default: false
    }
  },
  data() {
    return {
      isCollapse: true,
      isExtendStatus: false,
      sideWidth: "300px"
    };
  },
  methods: {
    handleCollapse(type) {
      if (!this.isExtend) {
        this.isCollapse = !this.isCollapse;
        return;
      }
      if (type === "lf") {
        if (this.sideWidth === "50%") {
          this.sideWidth = "300px";
          this.isCollapse = true;
          this.isExtendStatus = false;
        } else {
          this.sideWidth = "30px";
          this.isCollapse = false;
          this.isExtendStatus = false;
        }
      } else if (type === "rf") {
        this.sideWidth = "50%";
        this.isExtendStatus = true;
      } else if (type === "llf") {
        this.sideWidth = "300px";
        this.isExtendStatus = false;
      } else {
        this.sideWidth = "300px";
        this.isCollapse = true;
      }
      this.$emit("onExtend", this.sideWidth, this.isCollapse);
    }
  }
};
</script>

<style lang="scss" scoped>
.temp--aside-container {
  position: relative;
  height: 100%;
  margin-right: 10px;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  border: 1px solid #ddd;
  border-image: linear-gradient(180deg, #2e5b7c, #01aaf2) 3 3;
  box-shadow: unset;
  background: var(--logo-bg) no-repeat top right,
    linear-gradient(360deg, rgba(0, 68, 115, 0.64) 0%, rgba(0, 72, 122, 0.16) 100%);
  //   background: #13263a;
  .temp-aside-title {
    padding: 10px;
    box-sizing: border-box;
    border-bottom: 1px solid rgba(255, 255, 255, 0.4);
    position: relative;
    .temp-aside-collapse {
      margin-left: auto;
    }
    .is-collapse-icon {
      color: #fff;
    }
  }
  .temp-aside-main {
    padding: 10px;
    box-sizing: border-box;
    flex: 1 !important;
    overflow: auto;
  }
  .temp-aside-footer {
    height: 80px;
    padding: 10px;
    box-sizing: border-box;
    color: rgba(255, 255, 255, 0.6);
    border-top: 1px solid rgba(255, 255, 255, 0.4);
    font-size: 14px;
  }
  .temp-aside-off {
    height: 100%;
    cursor: pointer;
    .temp-aside-off-item {
      position: absolute;
      width: 100%;
      top: 34%;
      left: 0;
      display: flex;
      flex-direction: column;
      align-items: center;
      padding-top: 20px;
      box-sizing: border-box;
    }
  }
  .is-collapse-icon {
    font-size: 16px;
    cursor: pointer;
    color: #24deff;
  }
  .temp-nav-text {
    font-size: 14px;
    writing-mode: vertical-rl;
    letter-spacing: 0.5em;
    font-weight: 500;
    margin-top: 10px;
  }
}
.area-title {
  display: flex;
  font-size: 14px;
  align-items: center;
  position: relative;
}
.icon-collapse {
  position: absolute;
  top: 50%;
  height: 50px;
  right: -8px;
  width: 8px;
  transform: translateY(-50%);
  background-color: #fff;
  cursor: pointer;
  border-top-right-radius: 4px;
  border-bottom-right-radius: 4px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(12, 230, 250, 0.6);
  i {
    color: #fff;
    font-size: 12px;
  }
}
.dark {
  .temp-aside-title {
    background: linear-gradient(270deg, rgba(0, 202, 255, 0.68) 0%, rgba(0, 150, 255, 0) 100%);
    border-bottom: 1px solid;
    border-image: linear-gradient(180deg, rgba(116, 190, 243, 0), rgba(0, 180, 255, 0.6)) 1 1;
    color: #24deff;
  }
  .temp-aside-off {
    color: #24deff;
  }
}
.light {
  .temp--aside-container {
    background: #fff;
    box-shadow: 0 0 10px 0 rgba(144, 147, 153, 0.3);
    border: 1px solid #fff;
    color: #333 !important;
  }
  .area-title {
    color: #333;
  }
  .is-collapse-icon {
    color: #333;
  }
  .temp-aside-title {
    border-bottom: 1px solid #ccc;
    color: #0075e9;
    .is-collapse-icon {
      color: #0075e9;
    }
  }
  .temp-aside-off {
    color: #0075e9;
  }
  .temp-aside-footer {
    border-top: 1px solid #ccc;
    color: #333;
  }
  .icon-collapse {
    background: #0075e9;
  }
}
</style>
