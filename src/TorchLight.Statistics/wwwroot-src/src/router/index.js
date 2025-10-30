import { createRouter, createWebHashHistory } from 'vue-router'
import Home from '../views/Home.vue'
import MapList from '../views/MapList.vue'
import MapDetail from '../views/MapDetail.vue'
import Statistics from '../views/Statistics.vue'
import MapSettings from '../views/MapSettings.vue'

const routes = [
  {
    path: '/',
    name: 'Home',
    component: Home
  },
  {
    path: '/maps',
    name: 'MapList',
    component: MapList
  },
  {
    path: '/maps/:id',
    name: 'MapDetail',
    component: MapDetail,
    props: true
  },
  {
    path: '/statistics',
    name: 'Statistics',
    component: Statistics
  },
  {
    path: '/settings',
    name: 'MapSettings',
    component: MapSettings
  }
]

const router = createRouter({
  history: createWebHashHistory(),
  routes
})

export default router
