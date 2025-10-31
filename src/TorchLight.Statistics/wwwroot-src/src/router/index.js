import { createRouter, createWebHashHistory } from 'vue-router'
import Home from '../views/Home.vue'
import MapList from '../views/MapList.vue'
import MapDetail from '../views/MapDetail.vue'
import Statistics from '../views/Statistics.vue'
import Settings from '../views/Settings.vue'
import HistoryRecord from '../views/HistoryRecord.vue'
import HistoryDetail from '../views/HistoryDetail.vue'

const routes = [
  {
    path: '/',
    name: 'home',
    component: Home
  },
  {
    path: '/maps',
    name: 'maps',
    component: MapList
  },
  {
    path: '/maps/:id',
    name: 'map-detail',
    component: MapDetail
  },
  {
    path: '/statistics',
    name: 'statistics',
    component: Statistics
  },
  {
    path: '/history',
    name: 'history',
    component: HistoryRecord
  },
  {
    path: '/history/detail',
    name: 'history-detail',
    component: HistoryDetail
  },
  {
    path: '/settings',
    name: 'settings',
    component: Settings
  }
]

const router = createRouter({
  history: createWebHashHistory(),
  routes
})

export default router
