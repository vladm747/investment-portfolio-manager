import useMetadata from "./Hooks/UseMetadata";
import NavBar from "./Components/NavBar/NavBar";


function App() {
  const metadata = useMetadata();

  return(
    <>
      <NavBar/>
    </>
  );
}

export default App;
