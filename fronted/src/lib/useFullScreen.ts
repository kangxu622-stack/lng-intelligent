import { ref } from 'vue'

export function useFullScreen() {
  const isFullScreen = ref(false)

  const getFullscreenElement = () => {
    return (
      document.fullscreenElement ||
      (document as any).mozFullScreenElement ||
      (document as any).msFullScreenElement ||
      (document as any).webkitFullscreenElement ||
      null
    )
  }

  const toggleFullScreen = async (element: HTMLElement) => {
    if (getFullscreenElement()) {
      // 退出全屏
      if (document.exitFullscreen) {
        await document.exitFullscreen()
      } else if ((document as any).webkitCancelFullScreen) {
        (document as any).webkitCancelFullScreen()
      } else if ((document as any).mozCancelFullScreen) {
        (document as any).mozCancelFullScreen()
      } else if ((document as any).msExitFullscreen) {
        (document as any).msExitFullscreen()
      }
      isFullScreen.value = false
    } else {
      // 进入全屏
      if (element.requestFullscreen) {
        await element.requestFullscreen()
      } else if ((element as any).webkitRequestFullScreen) {
        (element as any).webkitRequestFullScreen()
      } else if ((element as any).mozRequestFullScreen) {
        (element as any).mozRequestFullScreen()
      } else if ((element as any).msRequestFullscreen) {
        (element as any).msRequestFullscreen()
      }
      isFullScreen.value = true
    }
  }

  // 监听全屏变化事件
  const initFullScreenListener = (callback?: (isFull: boolean) => void) => {
    const events = ['fullscreenchange', 'webkitfullscreenchange', 'mozfullscreenchange', 'msfullscreenchange']
    const handler = () => {
      const isFull = !!getFullscreenElement()
      isFullScreen.value = isFull
      callback?.(isFull)
    }
    events.forEach(event => {
      document.addEventListener(event, handler)
    })
    return () => {
      events.forEach(event => {
        document.removeEventListener(event, handler)
      })
    }
  }

  return {
    isFullScreen,
    getFullscreenElement,
    toggleFullScreen,
    initFullScreenListener
  }
}
