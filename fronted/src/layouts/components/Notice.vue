<template>
  <t-popup
    expand-animation
    placement="bottom"
    trigger="click"
    :visible="isNoticeVisible"
    @visible-change="onPopupVisibleChange"
  >
    <template #content>
      <div class="header-msg">
        <div class="header-msg-top">
          <p>通知（报警数目：{{ total }}）</p>
        </div>
        <div
          v-if="tableData.length > 0"
          ref="listDiv"
          style="height: 400px; overflow-y: scroll;"
          class="narrow-scrollbar"
          :split="true"
        >
          <t-list-item
            v-for="(item, index) in tableData"
            :key="index"
            :style="{
              color: item.typeColor,
              borderBottom: settingStore.mode === 'light' ? '1px solid #eee' : '1px solid gray',
            }"
          >
            <div class="g-w100 g-h100" style="padding: 8px 16px;" @click="sureWarn(item)">
              <div class="g-row-flex">
                <div class="g-row-flex">
                  <p class="msg-level" :style="{ background: item.typeColor }">
                    {{ item.levelName }}
                  </p>
                  <p class="msg-type" :style="{ color: settingStore.mode === 'dark' ? '#fff' : '#000' }">
                    {{ item.typeName }}
                  </p>
                </div>
                <p class="msg-time">
                  {{ item.alarmTime }}
                </p>
              </div>
              <p class="msg-content" v-html="item.alarmContent" />
            </div>
            <template #action>
              <t-button size="small" variant="outline" @click="sureWarn(item)">
                {{ item.delType === '1' ? '确认' : '处理' }}
              </t-button>
            </template>
          </t-list-item>
          <div class="g-row-flex-H">
            加载中...
          </div>
        </div>

        <div v-else class="empty-list">
          <img src="../../assets/intelligentOilfield/nothing.png" alt="空">
          <p>暂无通知</p>
        </div>
        <div class="header-msg-bottom">
          <t-button
            class="header-msg-bottom-link"
            variant="text"
            theme="primary"
            @click="goDetail"
          >
            查看全部
          </t-button>
        </div>
      </div>
    </template>
    <t-badge
      :count="total"
      :offset="[12, 3]"
    >
      <t-button
        theme="default"
        shape="square"
        variant="text"
        style="background: transparent; border: 0;"
        @click="updateData"
      >
        <svg-icon
          :icon-class="settingStore.mode === 'light' ? 'notice-new' : 'notice-new-dark'"
          class="panelIconClass"
        />
      </t-button>
    </t-badge>
  </t-popup>
</template>

<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount } from 'vue'
import { useSettingStore } from '@/store/modules/setting'
import { ElMessageBox } from 'element-plus'
import SvgIcon from '@/components/svg-icon/index.vue'
// import {
//   queryAlcAlarmByParam,
//   updateAlcAlarmCheckTag,
//   popoverRingMessage
// } from "@/api/intelligentOilfield/portal/projectionMode" // 暂时注释，如果 API 不存在
import proxy from '@/config/host'
import jumpSupApp from '@/utils/jumpSupApp.js'

const env = import.meta.env.MODE

const settingStore = useSettingStore()

// const scrollData = ref(0)
const tableData = ref<any[]>([])
const isNoticeVisible = ref(false)
const queryParams = ref({
  pageNum: 1,
  pageSize: 10
})
const timer = ref<number | null>(null)
const total = ref(0)
const listDiv = ref<HTMLElement>()

const handleScroll = (e: Event) => {
  const target = e.target as HTMLElement
  if (target.scrollTop + target.clientHeight >= target.scrollHeight && isNoticeVisible.value) {
    queryParams.value.pageNum += 1
    const maxPageNum = Math.ceil(total.value / 10)
    if (queryParams.value.pageNum <= maxPageNum) {
      getList(false)
    }
  }
}

const pollingTime = () => {
  if (timer.value) {
    window.clearInterval(timer.value)
  }
  timer.value = window.setInterval(() => {
    setTimeout(() => {
      getList(true)
      if (listDiv.value) listDiv.value.scrollTop = 0
    }, 0)
  }, 60000)
}

const sureWarn = (row: any) => {
  if (row.delType === '1') {
    // const queryParam = {
    //   alarmId: row.alarmId
    // }
    ElMessageBox.confirm('是否已确定告警内容？', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    }).then(() => {
      // 暂时注释 API 调用
      // updateAlcAlarmCheckTag(queryParam).then(() => {
      //   getList(true)
      // })
      getList(true)
    }).catch(() => {})
  } else {
    // 暂时注释，因为需要 token
    // window.open(`${proxy[env].ALARM_URL}${settingStore.token}`, "_blank")
  }
}

const updateData = () => {
  getList(true)
}

const getList = (firstPage: boolean) => {
  if (firstPage) {
    queryParams.value.pageNum = 1
  }
  // 暂时注释 API 调用
  // if (process.env.NODE_ENV === "release") {
  //   const param = {
  //     pageNum: 1,
  //     pageSize: 10
  //   }
  //   const currentParam = firstPage ? param : queryParams.value
  //   queryAlcAlarmByParam(currentParam, false).then(response => {
  //     const _res = JSON.parse(JSON.stringify(response.data.rows))
  //     if (firstPage) {
  //       queryParams.value.pageNum = 1
  //       tableData.value = _res
  //       total.value = response.data.total
  //     } else {
  //       tableData.value = [...tableData.value, ..._res]
  //     }
  //   })
  // }
}

const onPopupVisibleChange = (visible: boolean, context: any) => {
  if (!visible) {
    if (listDiv.value) listDiv.value.scrollTop = 0
  }
  if (context.trigger === 'trigger-element-click') {
    isNoticeVisible.value = true
    return
  }
  isNoticeVisible.value = visible
}

const goDetail = () => {
  jumpSupApp(proxy[env as keyof typeof proxy].MESSAGE_URL)
  isNoticeVisible.value = false
}

onMounted(() => {
  window.addEventListener('scroll', handleScroll, true)
  getList(true)
  pollingTime()
})

onBeforeUnmount(() => {
  if (timer.value) {
    window.clearInterval(timer.value)
  }
  window.removeEventListener('scroll', handleScroll, true)
})
</script>

<style>
.t-badge--circle {
  color: transparent;
  width: 10px;
  height: 11px;
}
</style>
<style lang="less" scoped>
@import "@/style/variables.less";

.t-popup__content {
  padding: 0;

  .header-msg {
    width: 495px;
    height: 500px;

    .empty-list {
      height: calc(100% - 104px);
      text-align: center;
      padding-top: 135px;
      font-size: 14px;
      color: var(--td-text-color-secondary);

      img {
        width: 63px;
      }

      p {
        margin-top: 30px;
      }
    }

    &-top {
      position: relative;
      height: 56px;
      font-size: 16px;
      color: var(--td-text-color-primary);
      text-align: center;
      line-height: 56px;
      border-bottom: 1px solid var(--td-component-border);
    }

    &-bottom {
      height: 48px;
      align-items: center;
      display: flex;
      justify-content: center;

      &-link {
        text-decoration: none;
        font-size: 14px;
        color: var(--td-brand-color);
        line-height: 48px;
        cursor: pointer;
      }
    }

    .t-list {
      height: calc(100% - 104px);
    }

    .t-list-item {
      overflow: hidden;
      width: 100%;
      padding: 0;
      border-radius: @border-radius;
      font-size: 14px;
      color: var(--td-text-color-primary);
      line-height: 18px;
      cursor: pointer;

      &:hover {
        transition: background 0.2s ease;
        background: var(--td-bg-color-container-hover);

        .msg-content {
          color: var(--td-brand-color-8);
          font-size: 12px;
          margin-top: 5px;
        }

        .t-list-item__action {
          button {
            bottom: 16px;
            opacity: 1;
          }
        }

        .msg-time {
          bottom: -6px;
          opacity: 0;
          font-size: 10px;
        }
      }

      .msg-content {
        margin-bottom: 10px;
        font-size: 12px;
        margin-top: 5px;
      }

      .msg-type {
        color: var(--td-text-color-secondary);
        font-size: 10px;
        width: 289px;
      }

      .msg-level {
        color: #fff;
        width: fit-content;
        border-radius: 4px;
        font-size: 10px;
        padding: 1px 10px;
        margin-right: 5px;
        transform: scale(0.83);
      }

      .t-list-item__action {
        button {
          opacity: 0;
          position: absolute;
          right: 24px;
          top: 5px;
        }
      }

      .msg-time {
        transition: all 0.2s ease;
        opacity: 1;
        color: var(--td-text-color-secondary);
        font-size: 10px;
      }
    }
  }
}
</style>
