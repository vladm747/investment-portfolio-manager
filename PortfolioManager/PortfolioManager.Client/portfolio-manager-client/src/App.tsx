import React, {lazy} from "react";
import {BrowserRouter as Router, Route, Routes, useLocation} from "react-router-dom";
import SignIn from "./Components/SignIn/SignIn";
import SignUp from "./Components/SignUp/SignUp";
import PortfoliosPage from "./Components/Portfolios/PortfoliosPage";
import AppShell from "./Components/Layout/AppShell";
import ErrorBoundary from "./Components/Layout/ErrorBoundary";
import NotFoundPage from "./Components/Layout/NotFoundPage";
import {NotificationsProvider} from "./Components/Shared/Notifications";
import {AuthProvider} from "./Auth/AuthProvider";
import {GuestOnly, RequireAuth} from "./Auth/RouteGuards";
import {ThemeModeProvider} from "./Theme/ThemeModeProvider";

// The two pages with charts load on demand, so the sign-in screen does not download Recharts.
const PortfolioDashboard = lazy(() => import("./Components/PortfolioDashboard/PortfolioDashboard"));
const OptimisationPage = lazy(() => import("./Components/Optimisation/OptimisationPage"));

/** A crash in one page is cleared when the user goes to another. */
function RoutedErrorBoundary({children}: {children: React.ReactNode}) {
    const {pathname} = useLocation();
    return <ErrorBoundary resetKey={pathname}>{children}</ErrorBoundary>;
}

function App() {
  return(
    <ThemeModeProvider>
        <NotificationsProvider>
            <AuthProvider>
                <Router>
                    <RoutedErrorBoundary>
                        <Routes>
                            <Route element={<GuestOnly/>}>
                                <Route path="/login" element={<SignIn/>} />
                                <Route path="/sign-up" element={<SignUp/>} />
                            </Route>
                            <Route element={<AppShell/>}>
                                <Route element={<RequireAuth/>}>
                                    <Route path="/" element={<PortfoliosPage/>} />
                                    <Route path="/portfolios/:id" element={<PortfolioDashboard/>} />
                                    <Route path="/portfolios/:id/optimisation" element={<OptimisationPage/>} />
                                </Route>
                                <Route path="*" element={<NotFoundPage/>} />
                            </Route>
                        </Routes>
                    </RoutedErrorBoundary>
                </Router>
            </AuthProvider>
        </NotificationsProvider>
    </ThemeModeProvider>
  );
}

export default App;
