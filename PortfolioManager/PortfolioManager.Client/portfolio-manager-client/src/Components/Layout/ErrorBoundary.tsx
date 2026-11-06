import React from 'react';
import Container from '@mui/material/Container';
import ErrorState from '../Shared/ErrorState';

interface Props {
    children: React.ReactNode;
    /** When this changes (the route), a shown error is cleared so the next page gets a fresh start. */
    resetKey?: string;
}

interface State {
    failed: boolean;
}

/** Catches a crash while a page renders and shows a message instead of a blank screen. */
export default class ErrorBoundary extends React.Component<Props, State> {
    state: State = { failed: false };

    static getDerivedStateFromError(): State {
        return { failed: true };
    }

    componentDidCatch(error: unknown) {
        console.error('The page crashed', error);
    }

    componentDidUpdate(previous: Props) {
        if (this.state.failed && previous.resetKey !== this.props.resetKey) {
            this.setState({ failed: false });
        }
    }

    render() {
        if (!this.state.failed) {
            return this.props.children;
        }
        return (
            <Container maxWidth="sm">
                <ErrorState
                    title="Щось пішло не так"
                    message="Сторінка не змогла відобразитися. Спробуйте оновити її; якщо помилка повториться, поверніться до списку портфелів."
                    onRetry={() => window.location.reload()}
                />
            </Container>
        );
    }
}
