<template>
  <div>
    <el-dialog
      v-if="dialogVisible"
      title="站内信列表"
      v-model="dialogVisible"
      width="1300px"
      append-to-body
      @closed="closeDialog"
    >
      <div style="height: 93%; padding-left: 13px;">
        <!-- <station-message ref="messageRef" /> -->暂时注释，如果组件不存在
      </div>
      <div
        slot="footer"
        class="dialog-footer"
        style="border-top: 1px solid var(--light-blue-color); padding-top: 20px;"
      >
        <el-button type="primary" @click="closeDialog">
          关 闭
        </el-button>
      </div>
    </el-dialog>
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
            <p>通知（通知数目：{{ unreadMsg.length }}）</p>
            <t-button
              v-if="unreadMsg.length > 0"
            class="clear-btn"
            variant="text"
            theme="primary"
            @click="setRead('all')"
          >
              全部已读
            </t-button>
        </div>
          <t-list v-if="unreadMsg.length > 0" class="narrow-scrollbar" :split="true">
            <t-list-item v-for="(item, index) in unreadMsg" :key="index">
              <div>
                <p class="msg-content">
                  内容：{{ item.content }}
                </p>
                <p class="msg-type">
                  类型：{{ item.type }}
                  </p>
                </div>
                <p class="msg-time">
                {{ item.date }}
                </p>
            <template #action>
                <t-button size="small" variant="outline" @click="setRead('radio', item)">
                  设为已读
              </t-button>
            </template>
          </t-list-item>
          </t-list>

        <div v-else class="empty-list">
          <!-- <img src="../../assets/intelligentOilfield/nothing.png" alt="空"> -->
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
      <t-badge :count="unreadMsg.length" :offset="[10, 3]">
      <t-button
        theme="default"
        shape="square"
        variant="text"
        style="background: transparent; border: 0;"
          @click="isNoticeVisible = true"
      >
        <svg-icon
            :icon-class="settingStore.mode === 'light' ? 'message-new' : 'message-new-dark'"
          class="panelIconClass"
        />
      </t-button>
    </t-badge>
  </t-popup>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useSettingStore } from '@/store/modules/setting'
import SvgIcon from '@/components/svg-icon/index.vue'
// import { getByTenantId, updateAllStatus, updateOneStatus } from "@/api/intelligentOilfield/system/notification.js" // 暂时注释，如果 API 不存在
import type { NotificationItem } from '@/interface'
// import stationMessage from "@/pages/intelligentOilfield/stationMessage/index.vue" // 暂时注释，如果组件不存在

const settingStore = useSettingStore()

const isNoticeVisible = ref(false)
const dialogVisible = ref(false)
// const messageRef = ref() // 暂时未使用
const msgData = ref<NotificationItem[]>([])

// 计算未读消息
const unreadMsg = computed(() => {
  return msgData.value.filter(item => !item.status)
})

const getList = () => {
  // 暂时注释 API 调用
  // getByTenantId({
  //   userId: settingStore.userId, status: "0", pageNum: 1,
  //   pageSize: 100000
  // }).then(res => {
  //   msgData.value = res.data.rows
  // })
}

const onPopupVisibleChange = (visible: boolean, context: any) => {
  if (context.trigger === 'trigger-element-click') {
    isNoticeVisible.value = true
    return
  }
  isNoticeVisible.value = visible
}

const closeDialog = () => {
  dialogVisible.value = false
  // messageRef.value?.resetData() // 暂时注释，如果组件不存在
}

const goDetail = () => {
  dialogVisible.value = true
  isNoticeVisible.value = false
}

const setRead = (type: string, item?: NotificationItem) => {
  if (type === 'all') {
    // 暂时注释 API 调用
    // updateAllStatus(settingStore.userId).then(() => {
    //   getList()
    // })
    msgData.value.forEach(e => {
      e.status = false
    })
  } else if (item) {
    // 暂时注释 API 调用
    // updateOneStatus({ userId: settingStore.userId, mailId: item.mailId }).then(() => {
    //   getList()
    // })
    msgData.value.forEach(e => {
          if (e.id === item.id) {
        e.status = false
          }
    })
    }
  }

onMounted(() => {
  getList()
})
</script>

<style lang="less" scoped>
@import "@/style/variables.less";

  .header-msg {
  width: 400px;
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

      .clear-btn {
        position: absolute;
        top: 12px;
        right: 24px;
      }
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
    padding: 16px 24px;
      border-radius: @border-radius;
      font-size: 14px;
      color: var(--td-text-color-primary);
    line-height: 22px;
      cursor: pointer;

      &:hover {
        transition: background 0.2s ease;
        background: var(--td-bg-color-container-hover);

        .msg-content {
          color: var(--td-brand-color-8);
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
        }
      }

      .msg-content {
      margin-bottom: 16px;
      }

      .msg-type {
        color: var(--td-text-color-secondary);
      }

      .t-list-item__action {
        button {
          opacity: 0;
          position: absolute;
          right: 24px;
        bottom: -6px;
        }
      }

      .msg-time {
        transition: all 0.2s ease;
        opacity: 1;
      position: absolute;
      right: 24px;
      bottom: 16px;
        color: var(--td-text-color-secondary);
    }
  }
}
</style>
<style scoped>
.operations-container .t-button {
  margin: 0 5px;
}
</style>
