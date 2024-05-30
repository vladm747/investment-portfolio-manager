import {useState} from "react";

function UseMetadata() {
    const [title, setTitle] = useState<string>('Portfolio Manager')
return {title};
}

export default UseMetadata;
