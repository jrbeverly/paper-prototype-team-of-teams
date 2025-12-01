import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '../views/HomeView.vue'
import TeamsView from '../views/TeamsView.vue'
import TeamDetailView from '../views/TeamDetailView.vue'
import SignInView from '../views/SignInView.vue'
import SignUpView from '../views/SignUpView.vue'
import ProfileView from '../views/ProfileView.vue'
import PeopleView from '../views/PeopleView.vue'
import PersonView from '../views/PersonView.vue'

export default createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', component: HomeView },
    { path: '/teams', component: TeamsView },
    { path: '/teams/:id', component: TeamDetailView },
    { path: '/sign-in', component: SignInView },
    { path: '/sign-up', component: SignUpView },
    { path: '/profile', component: ProfileView },
    { path: '/people', component: PeopleView },
    { path: '/people/:id', component: PersonView },
  ],
})
