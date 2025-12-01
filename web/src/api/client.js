import { getIdToken } from '../auth/cognito.js'

const BASE_URL = import.meta.env.VITE_API_URL ?? ''
const LOCAL_PERSON_ID = import.meta.env.VITE_LOCAL_PERSON_ID

async function request(path, options = {}) {
  const token = await getIdToken()
  const headers = { ...options.headers }
  if (token) headers['Authorization'] = `Bearer ${token}`
  if (LOCAL_PERSON_ID) headers['X-Person-Id'] = LOCAL_PERSON_ID
  const res = await fetch(`${BASE_URL}${path}`, { ...options, headers })
  if (!res.ok) throw new Error(`HTTP ${res.status}`)
  if (res.status === 204) return null
  return res.json()
}

export const healthCheck = () => request('/health')
export const listTeams = () => request('/teams')
export const getTeam = (id) => request(`/teams/${id}`)
export const getMe = () => request('/me')
export const patchMe = (data) =>
  request('/me', {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data),
  })
export const listPeople = () => request('/people')
export const getPerson = (id) => request(`/people/${id}`)
export const joinTeam = (id) =>
  request(`/teams/${id}/join`, { method: 'POST' })
export const leaveTeam = (id) =>
  request(`/teams/${id}/leave`, { method: 'POST' })
export const getMyMembership = () => request('/me/membership')
export const getMyObservations = () => request('/me/observations')
export const observeTeam = (id) => request(`/teams/${id}/observe`, { method: 'POST' })
export const unobserveTeam = (id) => request(`/teams/${id}/observe`, { method: 'DELETE' })
export const assignRole = (teamId, data) =>
  request(`/teams/${teamId}/roles`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data),
  })
export const removeRole = (teamId, role, personId) =>
  request(
    `/teams/${teamId}/roles/${encodeURIComponent(role)}?personId=${encodeURIComponent(personId)}`,
    { method: 'DELETE' },
  )
export const actionTransfer = (id, action, personId) =>
  request(`/transfers/${id}`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json', 'X-Person-Id': personId },
    body: JSON.stringify({ action }),
  })
export const cancelTransfer = (id) =>
  request(`/transfers/${id}`, { method: 'DELETE' })
export const getPendingTransfers = (personId) =>
  request('/transfers/pending', { headers: { 'X-Person-Id': personId } })
