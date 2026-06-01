<template>
  <div ref="resize" class="resize">
    <span ref="resizeHandle" class="handle-resize" />
    <slot />
  </div>
</template>
<script>
export default {
  name: "ResizeBox",
  props: {
    resizeConf: {
      type: Object,
      default: () => ({
        width: 300, // 初始宽度
        widthRange: [300, 10000] // 宽度范围
      })
    }
  },
  mounted() {
    this.dragControllerDiv(this.$refs.resize, this.$refs.resizeHandle);
  },
  methods: {
    dragControllerDiv(resizeBox, resizeHandle) {
      resizeBox.style.width = `${this.resizeConf.width}px`;
      // 鼠标按下事件
      resizeHandle.onmousedown = e => {
        const resizeWidth = resizeBox.offsetWidth;
        const startX = e.clientX; // 水平坐标
        // 鼠标拖动事件
        document.onmousemove = ev => {
          const moveX = ev.clientX;
          const moveLen = resizeWidth + (moveX - startX);
          if (
            this.resizeConf.widthRange[0] <= moveLen &&
            this.resizeConf.widthRange[1] >= moveLen
          ) {
            resizeBox.style.width = `${moveLen}px`;
          }
        };
        // 鼠标松开事件
        document.onmouseup = function() {
          document.onmousemove = null;
          document.onmouseup = null;
        };
      };
    }
  }
};
</script>
<style lang="scss" scoped>
.resize {
  position: relative;
  word-wrap: break-word;
  .handle-resize {
    cursor: col-resize;
    position: absolute;
    right: -2px;
    width: 6px;
    height: 50px;
    border-left: 2px solid #c5c5c5;
    border-right: 2px solid #c5c5c5;
    top: calc(50% - 25px);
  }
}
</style>