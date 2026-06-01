<template>
  <div class="custom--slider-wrap" :class="(border ? 'border' : '', showBg ? 'bg' : '')">
    <div class="align-center flex1">
      <i v-if="showButton" class="el-icon-remove-outline" @click="handleChange('remove')" />
      <el-slider v-model="value" class="flex1" style="margin: 0 10px" @change="handleChange('change')"></el-slider>
      <i v-if="showButton" class="el-icon-circle-plus-outline" @click="handleChange('plus')" />
    </div>
    <span class="ml10" style="width: 30px">{{ numberValue }}</span>
  </div>
</template>

<script>
export default {
  components: {},
  props: {
    numberValue: {
      type: Number,
      default: 0,
    },
    step: {
      type: Number,
      default: 10,
    },
    max: {
      type: Number,
      default: 100,
    },
    min: {
      type: Number,
      default: 0,
    },
    showButton: {
      type: Boolean,
      default: true,
    },
    showBg: {
      type: Boolean,
      default: false,
    },
  },
  data() {
    return {
      value: 0,
    };
  },
  computed: {
    border() {
      return Object.prototype.hasOwnProperty.call(this.$attrs, "border");
    },
  },
  created() {
    this.value = this.numberValue;
  },
  methods: {
    handleChange(type) {
      if (type === "remove") {
        this.value -= this.step;
      }
      if (type === "plus") {
        this.value += this.step;
      }
      if (this.value > this.max) {
        this.value = this.max;
      }
      if (this.value < this.min) {
        this.value = this.min;
      }
      this.$emit("update:numberValue", Number.isInteger(this.value) ? this.value : Number(this.value.toFixed(2)));
    },
  },
};
</script>
<style lang="scss" scoped>
// 自定义滑块样式（带加减）
.custom--slider-wrap {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  position: relative;
  padding-left: 10px;
  box-sizing: border-box;

  i {
    font-size: 20px;
    cursor: pointer;
  }
}
.light {
  .bg {
    background: #fff !important;
    box-shadow: 0 0 10px 0 rgba(144, 147, 153, 0.3) !important;
  }
}
.bg {
  background: var(--logo-bg) no-repeat top right,
    linear-gradient(360deg, rgba(0, 68, 115, 0.64) 0%, rgba(0, 72, 122, 0.16) 100%);
}
.border {
  border: 1px solid var(--border-color-2);
}
</style>
