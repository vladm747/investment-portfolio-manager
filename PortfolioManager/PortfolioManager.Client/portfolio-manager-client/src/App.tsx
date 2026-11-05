import NavBar from "./Components/NavBar/NavBar";
import {BrowserRouter as Router, Navigate, Route, Routes} from "react-router-dom";
import SignIn from "./Components/SignIn/SignIn";
import SignUp from "./Components/SignUp/SignUp";
import PortfolioDashboard from "./Components/PortfolioDashboard/PortfolioDashboard";
import HomeComponent from "./Components/Home/Home";
import ComparisonView from "./Components/PortfolioDashboard/OptimisationView/ComparisonView";
import {AuthProvider} from "./Auth/AuthProvider";
import {GuestOnly, RequireAuth} from "./Auth/RouteGuards";

function App() {
  return(
    <AuthProvider>
        <Router>
            <NavBar/>
            <Routes>
                <Route element={<RequireAuth/>}>
                    <Route path="/" element={<HomeComponent/>} />
                    <Route path="/portfolio" element={<PortfolioDashboard/>} />
                    <Route path="/comparison/:portfolioId" element={<ComparisonView/>} />
                </Route>
                <Route element={<GuestOnly/>}>
                    <Route path="/login" element={<SignIn/>} />
                    <Route path="/sign-up" element={<SignUp/>} />
                </Route>
                <Route path="*" element={<Navigate to="/" replace/>} />
            </Routes>
        </Router>
    </AuthProvider>
  );
}

export default App;
