<template>
  <div ref="answerPanelRef" class="answer-panel">
    <el-scrollbar class="answer-scrollbar">
      <div class="message-list">
        <div
          v-for="message in messages"
          :key="message.id"
          class="message-row"
          :class="message.role"
        >
          <div class="message-bubble">
            <div class="message-content">{{ message.content }}</div>
          </div>
        </div>
      </div>
    </el-scrollbar>
    <transition name="scroll-bottom-fade">
      <button
        v-if="showScrollToBottom"
        class="scroll-to-bottom-btn"
        @click="handleScrollToBottom"
      >
        <el-icon><Bottom /></el-icon>
        <span>回到底部</span>
      </button>
    </transition>
  </div>
</template>

<script setup lang="ts">
import { nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { Bottom } from '@element-plus/icons-vue'

export interface ChatMessageItem {
  id: string
  role: 'assistant' | 'user'
  content: string
}

const props = defineProps<{
  messages: ChatMessageItem[]
}>()

const answerPanelRef = ref<HTMLElement | null>(null)
const shouldAutoScroll = ref(true)
const showScrollToBottom = ref(false)
const autoScrollThreshold = 80
let scrollAnimationFrame = 0

const getScrollWrap = () => {
  return answerPanelRef.value?.querySelector('.el-scrollbar__wrap') as HTMLElement | null
}

const updateAutoScrollState = () => {
  const wrap = getScrollWrap()
  if (!wrap) return

  const distanceToBottom = wrap.scrollHeight - wrap.scrollTop - wrap.clientHeight
  shouldAutoScroll.value = distanceToBottom <= autoScrollThreshold
  if (shouldAutoScroll.value) {
    showScrollToBottom.value = false
  }
}

const animateScrollTo = (target: number, duration = 1500) => {
  const wrap = getScrollWrap()
  if (!wrap) return

  if (scrollAnimationFrame) {
    cancelAnimationFrame(scrollAnimationFrame)
  }

  const startTop = wrap.scrollTop
  const distance = target - startTop
  if (Math.abs(distance) < 1) {
    wrap.scrollTop = target
    return
  }

  const startTime = performance.now()
  const easeOutCubic = (progress: number) => 1 - Math.pow(1 - progress, 3)

  const step = (now: number) => {
    const elapsed = now - startTime
    const progress = Math.min(elapsed / duration, 1)
    wrap.scrollTop = startTop + distance * easeOutCubic(progress)

    if (progress < 1) {
      scrollAnimationFrame = requestAnimationFrame(step)
    } else {
      wrap.scrollTop = target
      scrollAnimationFrame = 0
    }
  }

  scrollAnimationFrame = requestAnimationFrame(step)
}

const scrollToBottom = async (immediate = false) => {
  await nextTick()
  const wrap = getScrollWrap()
  if (!wrap) return

  if (immediate) {
    if (scrollAnimationFrame) {
      cancelAnimationFrame(scrollAnimationFrame)
      scrollAnimationFrame = 0
    }
    wrap.scrollTop = wrap.scrollHeight
  } else {
    animateScrollTo(wrap.scrollHeight)
  }
  shouldAutoScroll.value = true
  showScrollToBottom.value = false
}

const handleScrollToBottom = () => {
  scrollToBottom()
}

onMounted(async () => {
  await nextTick()
  const wrap = getScrollWrap()
  wrap?.addEventListener('scroll', updateAutoScrollState, { passive: true })
  updateAutoScrollState()
  scrollToBottom(true)
})

onBeforeUnmount(() => {
  const wrap = getScrollWrap()
  wrap?.removeEventListener('scroll', updateAutoScrollState)
  if (scrollAnimationFrame) {
    cancelAnimationFrame(scrollAnimationFrame)
  }
})

watch(
  () => props.messages.length,
  () => {
    if (shouldAutoScroll.value) {
      scrollToBottom()
    } else {
      showScrollToBottom.value = true
    }
  },
  { immediate: true }
)

watch(
  () => props.messages.map(item => item.content).join('\n'),
  () => {
    if (shouldAutoScroll.value) {
      scrollToBottom()
    }
  }
)
</script>

<style scoped lang="scss">
.answer-panel {
  position: relative;
  flex: 1;
  min-height: 280px;
  overflow: hidden;
  border: 1px solid rgba(48, 188, 255, 0.75);
  background: rgba(4, 23, 44, 0.42);
}

.answer-scrollbar {
  height: 100%;
}

.message-list {
  min-height: 100%;
  padding: 20px 24px;
}

.message-row {
  display: flex;
  margin-bottom: 16px;
}

.message-row.user {
  justify-content: flex-end;
}

.message-bubble {
  max-width: min(72%, 880px);
  padding: 14px 16px;
  border: 1px solid rgba(57, 181, 255, 0.32);
  background: rgba(10, 39, 70, 0.66);
  color: #eaf4ff;
}

.message-row.user .message-bubble {
  background: rgba(28, 89, 160, 0.42);
}

.message-content {
  white-space: pre-wrap;
  line-height: 1.8;
  font-size: 15px;
}

.scroll-to-bottom-btn {
  position: absolute;
  right: 18px;
  bottom: 18px;
  height: 38px;
  padding: 0 14px;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  border: 1px solid rgba(48, 188, 255, 0.85);
  background: rgba(10, 39, 70, 0.92);
  color: #ffffff;
  cursor: pointer;
  box-shadow: 0 8px 18px rgba(0, 0, 0, 0.25);
  transition: background 0.2s ease, border-color 0.2s ease, transform 0.2s ease;
}

.scroll-to-bottom-btn:hover {
  border-color: #7bd7ff;
  background: rgba(20, 72, 122, 0.96);
  transform: translateY(-1px);
}

.scroll-bottom-fade-enter-active,
.scroll-bottom-fade-leave-active {
  transition: opacity 0.2s ease, transform 0.2s ease;
}

.scroll-bottom-fade-enter-from,
.scroll-bottom-fade-leave-to {
  opacity: 0;
  transform: translateY(8px);
}
</style>
