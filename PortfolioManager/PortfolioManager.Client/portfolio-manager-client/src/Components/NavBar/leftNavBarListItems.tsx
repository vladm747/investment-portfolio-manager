import {ListItemButton, ListItemIcon, ListItemText} from "@mui/material";
import React from "react";
import DashboardIcon from '@mui/icons-material/Dashboard';
import BarChartIcon from '@mui/icons-material/BarChart';
import {Link} from "react-router-dom";

export const listItems = (
    <React.Fragment>
        <ListItemButton component={Link} to="/portfolio">
            <ListItemIcon>
                <DashboardIcon />
            </ListItemIcon>
            <ListItemText primary="Portfolio" />
        </ListItemButton>
        <ListItemButton component={Link} to="/statistic">
            <ListItemIcon>
                <BarChartIcon />
            </ListItemIcon>
            <ListItemText primary="Statistic" />
        </ListItemButton>
    </React.Fragment>
);
