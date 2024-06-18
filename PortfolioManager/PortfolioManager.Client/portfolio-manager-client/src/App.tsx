import useMetadata from "./Hooks/UseMetadata";
import NavBar from "./Components/NavBar/NavBar";
import {BrowserRouter as Router, Navigate, Route, Routes} from "react-router-dom";
import SignIn from "./Components/SignIn/SignIn";
import SignUp from "./Components/SignUp/SignUp";
import PortfolioDashboard from "./Components/PortfolioDashboard/PortfolioDashboard";
import {Switch} from "@mui/material";
import {Fragment} from "react";
import HomeComponent from "./Components/Home/Home";
import PortfolioDto from "./DTO/PortfolioDto";
import ComparisonView from "./Components/PortfolioDashboard/OptimisationView/ComparisonView";

function App() {
  const metadata = useMetadata();
    // const PrivateRoute = ({ auth: { isAuthenticated }, children }) => {
    //     return isAuthenticated ? children : <Navigate to="/login" />;
    // };


  return(
    <>
        <Router>
            <Fragment>
            <NavBar/>
            <Routes>
                <Route path="/"  Component={HomeComponent} />
                <Route path="/login"  Component={SignIn} />
                <Route path="/sign-up" Component={SignUp} />
                <Route path="/portfolio" Component={PortfolioDashboard} />
                <Route path="/comparison/:portfolioId" element={<ComparisonView />} />

                {/*<Route path="/statistic" Component={StatisticDashboard} />*/}
            </Routes>
            </Fragment>
        </Router>
    </>
  );
}

export default App;
