/*
 * @description: 公共组件全局注册
 * @author：jiangze
 */
// 移除了 vue-splitpane 导入，因为它是 Vue 2 的包，Vue 3 不兼容
// 如果将来需要分割面板功能，可以使用 package.json 中已有的 splitpanes 包

const files = import.meta.glob("./*.vue");
const baseComponents = {};
Object.keys(files).forEach(key => {
  const fileName = key.replace(/(\.\/|\.vue)/g, "");
  baseComponents[fileName] = files[key];
});
// baseComponents.splitPane = splitPane; // 已移除，Vue 3 不兼容
export { baseComponents };
