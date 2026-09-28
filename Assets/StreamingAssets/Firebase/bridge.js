import { initializeApp } from "https://www.gstatic.com/firebasejs/12.19.0/firebase-app.js";
import { getAuth, setPersistence, browserLocalPersistence, signInAnonymously } from "https://www.gstatic.com/firebasejs/12.19.0/firebase-auth.js";
import { getFirestore, collection, doc, setDoc, getDocsFromServer } from "https://www.gstatic.com/firebasejs/12.19.0/firebase-firestore.js";

const app = initializeApp({
  apiKey: "AIzaSyDLhtdN0Of_uCXpOMFYmlEaTug2gTfWw-E",
  authDomain: "adeeb-63981.firebaseapp.com",
  projectId: "adeeb-63981",
  appId: "1:88161785150:web:b4eb971cea0436d947086f"
}, "adeeb-workflow");
const auth = getAuth(app);
const database = getFirestore(app);
let connection;
async function connect() {
  if (!connection) connection = (async () => {
    await setPersistence(auth, browserLocalPersistence);
    await auth.authStateReady();
    if (!auth.currentUser) await signInAnonymously(auth);
    return auth.currentUser;
  })().catch(error => { connection = null; throw error; });
  return connection;
}
window.AdeebFirebase = {
  async execute(operation, payload) {
    const user = await connect();
    const projects = collection(database, "users", user.uid, "projects");
    if (operation === "connect") return { userId: user.uid };
    if (operation === "save") {
      const project = JSON.parse(payload);
      if (!project.id || !Array.isArray(project.pages) || project.pages.length === 0) throw new Error("Invalid project.");
      await setDoc(doc(projects, project.id), project);
      return { userId: user.uid };
    }
    if (operation === "list") {
      const snapshot = await getDocsFromServer(projects);
      return { userId: user.uid, projects: snapshot.docs.map(item => item.data()) };
    }
    throw new Error("Unknown Firebase operation.");
  }
};
