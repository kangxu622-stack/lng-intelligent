<template>
	<div ref="scEcharts" :style="{height:height, width:width, overflow: 'visible'}"></div>
</template>

<script>
	import * as echarts from 'echarts';
	echarts.env.touchEventsSupported = false;
	echarts.env.wxa = false;
	import T from './echarts-theme-T.js';
	echarts.registerTheme('T', T);
	const unwarp = (obj) => obj && (obj.__v_raw || obj.valueOf() || obj);

	export default {
		...echarts,
		name: "scEcharts",
		props: {
			height: { type: String, default: "100%" },
			width: { type: String, default: "100%" },
			nodata: {type: Boolean, default: false },
			option: { type: Object, default: () => {} }
		},
		data() {
			return {
				isActivat: false,
				myChart: null,
				resizeObserver: null,
				resizeHandler: null
			}
		},
		watch: {
			option: {
				deep:true,
				handler (v) {
					if (this.myChart) {
						unwarp(this.myChart).setOption(this.normalizeOption(v));
					}
				}
			},
			
		},
		computed: {
			myOptions: function() {
				return this.normalizeOption(this.option || {});
			}
		},
		activated(){
			if(!this.isActivat){
				this.$nextTick(() => {
					if (this.myChart) {
						this.myChart.resize()
					}
				})
			}
		},
		deactivated(){
			this.isActivat = false;
		},
		mounted(){
			this.isActivat = true;
			this.$nextTick(() => {
				this.draw();
				
				// 在 draw() 执行后再设置 ResizeObserver
				this.resizeObserver = new ResizeObserver((entries) => {
					// 当DOM元素的宽高发生变化时执行回调函数
					if (this.myChart) {
						this.myChart.resize();
					}
				});
				if (this.$refs.scEcharts) {
					this.resizeObserver.observe(this.$refs.scEcharts);
				}
			});
		},
		beforeUnmount() {
			// Vue 3 生命周期钩子 - 清理资源
			if (this.resizeObserver) {
				this.resizeObserver.disconnect();
				this.resizeObserver = null;
			}
			if (this.resizeHandler) {
				window.removeEventListener('resize', this.resizeHandler);
				this.resizeHandler = null;
			}
			if (this.myChart) {
				this.myChart.dispose();
				this.myChart = null;
			}
		},
		methods: {
			normalizeOption(option){
				if (!option || typeof option !== 'object' || Array.isArray(option)) {
					return option || {};
				}
				const tooltip = option.tooltip && typeof option.tooltip === 'object' && !Array.isArray(option.tooltip)
					? option.tooltip
					: {};
				return {
					...option,
					tooltip: {
						appendToBody: true,
						confine: false,
						...tooltip
					}
				};
			},
			draw(){
				var myChart = echarts.init(this.$refs.scEcharts, 'T',{renderer: 'svg',locale:'ZH'});
				myChart.setOption(this.myOptions);
				this.myChart = myChart;
				// 保存 resize 处理函数以便后续清理
				this.resizeHandler = function(){
					if (myChart) {
						myChart.resize()
					}
				};
				window.addEventListener('resize', this.resizeHandler);
				
			},
			resize(){
				if (this.myChart) {
					this.myChart.resize();
				}
			}
		}
	}
</script>
