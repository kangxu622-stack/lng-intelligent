<template>
  <div class="temp--bar-container">
    <div class="temp--bar-container-main">
      <div v-for="item in menuData" :key="item.value" class="temp--bar-item-wrap">
        <div class="temp--bar-item">
          <div class="bar--item-wrap">
            <div
              v-for="val in item.menuList"
              :key="val.value"
              class="bar--item"
              :class="{ 'menu-select': val.value === activeName && val.isOpen }"
              :data-key="val.value"
              :style="{
                color: val.isOpen ? 'var(--td-text-color-primary)' : 'rgba(180,180,180,1)',
                cursor: val.isOpen ? 'pointer' : 'no-drop',
              }"
              @click="handleMenuClick(val)"
            >
              <i :class="['bar--icon', val.icon]" />
              <p>{{ val.name }}</p>
            </div>
          </div>
          <div class="bar--classify">
            <span v-if="item.step">第{{ item.step }}步：</span>
            <span>{{ item.name }}</span>
          </div>
        </div>
        <div class="bar--split-line"></div>
      </div>
    </div>
  </div>
</template>

<script>
export default {
  props: {
    menuList: {
      required: true,
      type: Object,
      default: function () {
        return {
          template: {
            name: "菜单模版",
            value: "template",
            menuList: {
              model: {
                name: "模板",
                value: "model",
                isOpen: false,
                icon: "el-icon-DocumentAdd",
                event: "onModel",
              },
            },
          },
        };
      },
    },
  },
  data() {
    return {
      activeName: "create",
    };
  },
  computed: {
    menuData() {
      return this.menuList;
    },
  },
  methods: {
    handleMenuClick(e) {
      if (e.isPage && e.isOpen) {
        this.activeName = e.value;
      }
      if (e.isOpen) {
        this.$emit("onEvent", { ...e });
      }
    },
  },
};
</script>

<style lang="scss" scoped>
.temp--bar-container {
  height: 90px;
  overflow: auto;
  .temp--bar-container-main {
    height: 100%;
    display: flex;
    align-items: center;
  }
  .temp--bar-item-wrap {
    display: flex;
    align-items: center;
    &:last-child {
      .bar--split-line {
        display: none;
      }
    }
  }
  .temp--bar-item {
    text-align: center;
    padding: 0 20px;
    box-sizing: border-box;
    font-size: 12px;
    cursor: default;
    .bar--item-wrap {
      display: flex;
      border-bottom: 1px solid var(--border-color-1);
      padding: 0 4px 4px 4px;
      box-sizing: border-box;
      margin-bottom: 4px;
      justify-content: center;
      .bar--item {
        margin-left: 15px;
        &:first-child {
          margin-left: 0;
        }
        p {
          white-space: nowrap;
          overflow: hidden;
          text-overflow: ellipsis;
        }
      }
    }
  }
  .bar--classify {
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }
  .bar--split-line {
    height: 35px;
    width: 1px;
    border-left: 1px solid var(--border-color-1);
    margin: 0 10px;
  }
  .bar--icon {
    font-size: 16px;
  }
}

.dark {
  .temp--bar-container {
    border-bottom: 1px solid var(--border-color-1);
    border-image: linear-gradient(180deg, #2e5b7c, #01aaf2) 3 3;
    box-shadow: unset;
    background: var(--logo-bg) no-repeat top right,
      linear-gradient(360deg, rgba(0, 68, 115, 0.64) 0%, rgba(0, 72, 122, 0.16) 100%);
  }
  .menu-select {
    color: #01aaf2 !important;
  }
}
.light {
  .temp--bar-container {
    background: #fff;
    box-shadow: 0px 1px 5px 0 var(--td-gray-color-4);
    // border-top: 1px solid var(--td-component-stroke);
  }
  .menu-select {
    color: #0075e9 !important;
  }
}
</style>
