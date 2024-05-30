import useMetadata from "./Hooks/UseMetadata";
import NavBar from "./Components/NavBar/NavBar";
import {BrowserRouter as Router, Route, Routes} from "react-router-dom";
import SignIn from "./Components/SignIn/SignIn";
import SignUp from "./Components/SignUp/SignUp";

function App() {
  const metadata = useMetadata();

  return(
    <>
        <Router>
            <NavBar/>
            <Routes>
                <Route path="/login"  Component={SignIn} />
                <Route path="/sign-up" Component={SignUp} />
            </Routes>
        </Router>
    </>
  );
}

export default App;
