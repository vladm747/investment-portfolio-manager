import PortfolioList from "./PortfolioList";
import {useEffect} from "react";
import * as React from "react";
import Paper from "@mui/material/Paper";
import Grid from "@mui/material/Grid";
import Container from "@mui/material/Container";
import Box from "@mui/material/Box";
import {ThemeProvider} from "@mui/material/styles";
import Typography from "@mui/material/Typography";

function HomeComponent(){
    useEffect(() => {
        console.log('Home component mounted');
    }, []);

    return(
        <>
            <Box sx={{ display: 'flex' }}>
                <Container maxWidth="lg">
                    <Typography variant="h3" color="text.primary" align="center">
                            {'Список портфелів'}
                        </Typography>
                        <Grid item xs={12}>
                            <Paper sx={{ p: 2, display: 'flex', flexDirection: 'column' }}>
                                <PortfolioList/>
                            </Paper>
                        </Grid>
                </Container>
            </Box>
        </>
    )
}

export default HomeComponent;
