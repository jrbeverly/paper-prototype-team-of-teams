import {
  CognitoUserPool,
  CognitoUser,
  AuthenticationDetails,
  CognitoUserAttribute,
} from 'amazon-cognito-identity-js'

const userPoolId = import.meta.env.VITE_COGNITO_USER_POOL_ID
const clientId = import.meta.env.VITE_COGNITO_CLIENT_ID
const localPersonId = import.meta.env.VITE_LOCAL_PERSON_ID

const pool =
  userPoolId && clientId
    ? new CognitoUserPool({ UserPoolId: userPoolId, ClientId: clientId })
    : null

export function signUp(email, password) {
  if (localPersonId) return Promise.resolve()
  return new Promise((resolve, reject) => {
    if (!pool) return reject(new Error('Cognito is not configured'))
    const attrs = [new CognitoUserAttribute({ Name: 'email', Value: email })]
    pool.signUp(email, password, attrs, null, (err, result) => {
      if (err) reject(err)
      else resolve(result)
    })
  })
}

export function confirmSignUp(email, code) {
  if (localPersonId) return Promise.resolve()
  return new Promise((resolve, reject) => {
    if (!pool) return reject(new Error('Cognito is not configured'))
    const user = new CognitoUser({ Username: email, Pool: pool })
    user.confirmRegistration(code, true, (err, result) => {
      if (err) reject(err)
      else resolve(result)
    })
  })
}

export function signIn(email, password) {
  if (localPersonId) return Promise.resolve()
  return new Promise((resolve, reject) => {
    if (!pool) return reject(new Error('Cognito is not configured'))
    const user = new CognitoUser({ Username: email, Pool: pool })
    const auth = new AuthenticationDetails({ Username: email, Password: password })
    user.authenticateUser(auth, {
      onSuccess: (session) => resolve(session),
      onFailure: reject,
    })
  })
}

export function signOut() {
  if (localPersonId) return
  const user = pool?.getCurrentUser()
  if (user) user.signOut()
}

export function getIdToken() {
  if (localPersonId) return Promise.resolve('local')
  return new Promise((resolve) => {
    const user = pool?.getCurrentUser()
    if (!user) return resolve(null)
    user.getSession((err, session) => {
      if (err || !session?.isValid()) return resolve(null)
      resolve(session.getIdToken().getJwtToken())
    })
  })
}
