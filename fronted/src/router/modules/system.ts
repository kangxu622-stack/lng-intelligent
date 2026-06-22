import Layout from '@/layouts/index.vue'

export default [
  {
    path: '/home',
    name: '\u9996\u9875',
    meta: { hidden: false, title: '\u9996\u9875', icon: 'home' },
    component: Layout,

    children: [
      {
        path: 'overview',
        name: '\u7cfb\u7edf\u603b\u89c8',
        meta: { hidden: false, title: '\u7cfb\u7edf\u603b\u89c8' },
        component: () => import('@/pages/HomeScreen/SystemOverview/index.vue')
      },
      {
        path: 'unload',
        name: '\u5378\u8239',
        meta: { hidden: false, title: '\u5378\u8239'},
        component: () => import('@/pages/HomeScreen/Unloading/index.vue')
      },
      {
        path: 'tank',
        name: 'LNG\u7f50\u533a',
        meta: { hidden: false, title: 'LNG\u7f50\u533a'},
        component: () => import('@/pages/HomeScreen/Tank/index.vue')
      },
      {
        path: 'compressor',
        name: '\u538b\u7f29\u673a',
        meta: { hidden: false, title: '\u538b\u7f29\u673a'},
        component: () => import('@/pages/HomeScreen/Compressor/index.vue')
      },
      {
        path: 'recondenser',
        name: '\u518d\u51b7\u51dd\u5668',
        meta: { hidden: false, title: '\u518d\u51b7\u51dd\u5668'},
        component: () => import('@/pages/HomeScreen/Recondenser/index.vue')
      },
      {
        path: 'export-area',
        name: '\u9ad8\u538b\u5916\u8f93\u533a',
        meta: { hidden: false, title: '\u9ad8\u538b\u5916\u8f93\u533a'},
        component: () => import('@/pages/HomeScreen/HighPressure/index.vue')
      },
      {
        path: 'seawater-pump',
        name: '\u6d77\u6c34\u6cf5',
        meta: { hidden: false, title: '\u6d77\u6c34\u6cf5'},
        component: () => import('@/pages/HomeScreen/SeawaterPump/index.vue')
      },
      {
        path: 'valve',
        name: '\u9600\u95e8',
        meta: { hidden: false, title: '\u9600\u95e8'},
        component: () => import('@/pages/HomeScreen/Valve/index.vue')
      }
    ]
  },
  {
    path: '/pressure-control',
    name: '\u538b\u529b\u63a7\u5236',
    component: Layout,
    meta: { hidden: false, title: '\u538b\u529b\u63a7\u5236', icon: 'pressControl', single: false },
    redirect: '/pressure-control/strategy',
    children: [
      {
        path: 'strategy',
        name: '\u538b\u529b\u63a7\u5236\u7b56\u7565',
        meta: { hidden: false, title: '\u538b\u529b\u63a7\u5236\u7b56\u7565' },
        component: () => import('@/pages/PressureControl/Strategy/index.vue')
      },
      {
        path: 'valve-curve',
        name: '\u9600\u95e8\u7279\u6027\u66f2\u7ebf',
        meta: { hidden: false, title: '\u9600\u95e8\u7279\u6027\u66f2\u7ebf'},
        component: () => import('@/pages/PressureControl/ValveCurve/index.vue')
      },
      {
        path: 'one-click-start-stop',
        name: '\u4e00\u952e\u542f\u505c\u7a0b\u5e8f',
        meta: { hidden: false, title: '\u4e00\u952e\u542f\u505c\u7a0b\u5e8f'},
        component: () => import('@/pages/PressureControl/OneKeyStart/index.vue')
      }
    ]
  },
  {
    path: '/initialize',
    component: Layout,
    meta: { hidden: false, title: '\u521d\u59cb\u6761\u4ef6\u8bbe\u7f6e', icon: 'initialCondition', single: true },
    children: [
      {
        path: '',
        name: '\u521d\u59cb\u6761\u4ef6\u8bbe\u7f6e',
        meta: { hidden: false, title: '\u521d\u59cb\u6761\u4ef6\u8bbe\u7f6e' },
        component: () => import('@/pages/InitialCondition/index.vue')
      }
    ]
  },
  {
    path: '/generate',
    component: Layout,
    meta: { hidden: false, title: '\u8c03\u5ea6\u65b9\u6848\u751f\u6210', icon: 'generate', single: true },
    children: [
      {
        path: '',
        name: '\u8c03\u5ea6\u65b9\u6848\u751f\u6210',
        meta: { hidden: false, title: '\u8c03\u5ea6\u65b9\u6848\u751f\u6210' },
        component: () => import('@/pages/Generate/index.vue')
      }
    ]
  },
  {
    path: '/scheme',
    name: '\u8c03\u5ea6\u65b9\u6848\u5c55\u793a',
    component: Layout,
    meta: { hidden: false, title: '\u8c03\u5ea6\u65b9\u6848\u5c55\u793a', icon: 'scheme', single: false },
    redirect: '/scheme/unload-scheduling',
    children: [
      {
        path: 'unload-scheduling',
        name: '\u5378\u8239\u8c03\u5ea6',
        meta: { hidden: false, title: '\u5378\u8239\u8c03\u5ea6' },
        component: () => import('@/pages/Scheme/Unload/index.vue')
      },
      {
        path: 'export-optimization',
        name: '\u5916\u8f93\u4f18\u5316',
        meta: { hidden: false, title: '\u5916\u8f93\u4f18\u5316' },
        component: () => import('@/pages/Scheme/index.vue')
      },
      {
        path: 'bog-forecast',
        name: 'BOG\u9884\u6d4b',
        meta: { hidden: false, title: 'BOG\u9884\u6d4b' },
        component: () => import('@/pages/Scheme/BOGPredict/index.vue')
      },
      {
        path: 'report',
        name: '\u65e5\u5fd7\u4e0e\u62a5\u8868',
        meta: { hidden: false, title: '\u65e5\u5fd7\u4e0e\u62a5\u8868' },
        component: () => import('@/pages/Scheme/Report/index.vue')
      }
    ]
  },
  {
    path: '/monitor',
    component: Layout,
    meta: { hidden: false, title: '\u8bbe\u5907\u72b6\u6001\u76d1\u6d4b', icon: 'conditionMonitor', single: true },
    children: [
      {
        path: '',
        name: '\u8bbe\u5907\u72b6\u6001\u76d1\u6d4b',
        meta: { hidden: false, title: '\u8bbe\u5907\u72b6\u6001\u76d1\u6d4b' },
        component: () => import('@/pages/EquipmentStatus/index.vue')
      }
    ]
  },
  {
    path: '/ai-assistant',
    name: '\u5927\u6a21\u578b\u8f85\u52a9\u51b3\u7b56',
    component: Layout,
    meta: { hidden: false, title: '\u5927\u6a21\u578b\u8f85\u52a9\u51b3\u7b56', icon: 'ai', single: false },
    redirect: '/ai-assistant/qa',
    children: [
      {
        path: 'qa',
        name: '\u667a\u80fd\u95ee\u7b54',
        meta: { hidden: false, title: '\u667a\u80fd\u95ee\u7b54' },
        component: () => import('@/pages/LLM/Question/index.vue')
      },
      {
        path: 'fault-diagnosis',
        name: '\u6545\u969c\u8bca\u65ad',
        meta: { hidden: false, title: '\u6545\u969c\u8bca\u65ad' },
        component: () => import('@/pages/LLM/FaultDiagnosis/index.vue')
      },
      {
        path: 'scheme-explain',
        name: '\u8c03\u5ea6\u65b9\u6848\u89e3\u91ca',
        meta: { hidden: false, title: '\u8c03\u5ea6\u65b9\u6848\u89e3\u91ca' },
        component: () => import('@/pages/LLM/SchemeExplanation/index.vue')
      },
      {
        path: 'model-test',
        name: '\u6a21\u578b\u8c03\u7528\u6d4b\u8bd5',
        meta: { hidden: false, title: '\u6a21\u578b\u8c03\u7528\u6d4b\u8bd5' },
        component: () => import('@/pages/LLM/TestCase/index.vue')
      },
      {
        path: 'training',
        name: '\u6559\u80b2\u8bad\u7ec3\u7cfb\u7edf',
        meta: { hidden: false, title: '\u6559\u80b2\u8bad\u7ec3\u7cfb\u7edf', icon: 'education' },
        redirect: '/ai-assistant/training/manuals',
        children: [
          {
            path: 'manuals',
            name: '\u624b\u518c\u77e5\u8bc6\u5e93',
            meta: { hidden: false, title: '\u624b\u518c\u77e5\u8bc6\u5e93' },
            component: () => import('@/pages/LLM/Training/ManualManagement/index.vue')
          },
          {
            path: 'knowledge',
            name: '\u77e5\u8bc6\u70b9\u7ba1\u7406',
            meta: { hidden: false, title: '\u77e5\u8bc6\u70b9\u7ba1\u7406' },
            component: () => import('@/pages/LLM/Training/KnowledgeManagement/index.vue')
          },
          {
            path: 'question-gen',
            name: '\u667a\u80fd\u51fa\u9898',
            meta: { hidden: false, title: '\u667a\u80fd\u51fa\u9898' },
            component: () => import('@/pages/LLM/Training/QuestionGeneration/index.vue')
          },
          {
            path: 'question-review',
            name: '\u9898\u76ee\u5ba1\u6838',
            meta: { hidden: false, title: '\u9898\u76ee\u5ba1\u6838' },
            component: () => import('@/pages/LLM/Training/QuestionReview/index.vue')
          },
          {
            path: 'self-training',
            name: '\u81ea\u5b66\u8bad\u7ec3',
            meta: { hidden: false, title: '\u81ea\u5b66\u8bad\u7ec3' },
            component: () => import('@/pages/LLM/Training/SelfTraining/index.vue')
          },
          {
            path: 'wrong-questions',
            name: '\u9519\u9898\u672c',
            meta: { hidden: false, title: '\u9519\u9898\u672c' },
            component: () => import('@/pages/LLM/Training/WrongQuestions/index.vue')
          },
          {
            path: 'analytics',
            name: '\u5b66\u4e60\u5206\u6790',
            meta: { hidden: false, title: '\u5b66\u4e60\u5206\u6790' },
            component: () => import('@/pages/LLM/Training/Analytics/index.vue')
          }
        ]
      }
    ]
  },
  {
    path: '/data-management',
    component: Layout,
    meta: { hidden: false, title: '\u6570\u636e\u7ba1\u7406', icon: 'dataManagement', single: true },
    children: [
      {
        path: '',
        name: '\u6570\u636e\u7ba1\u7406',
        meta: { hidden: false, title: '\u6570\u636e\u7ba1\u7406' },
        component: () => import('@/pages/DataManagement/index.vue')
      }
    ]
  },
  {
    path: '/system',
    name: '\u7cfb\u7edf\u7ef4\u62a4',
    component: Layout,
    meta: { hidden: false, title: '\u7cfb\u7edf\u7ef4\u62a4', icon: 'systemManagement', single: false },
    redirect: '/system/roles-management',
    children: [
      {
        path: 'roles-management',
        name: '\u89d2\u8272\u7ba1\u7406',
        meta: { hidden: false, title: '\u89d2\u8272\u7ba1\u7406' },
        component: () => import('@/pages/System/rolesManagement/index.vue')
      },
      {
        path: 'user-management',
        name: '\u7528\u6237\u7ba1\u7406',
        meta: { hidden: false, title: '\u7528\u6237\u7ba1\u7406' },
        component: () => import('@/pages/System/userManagement/index.vue')
      }
    ]
  }
]
