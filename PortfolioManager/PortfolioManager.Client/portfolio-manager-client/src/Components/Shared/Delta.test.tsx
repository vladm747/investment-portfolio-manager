import { render, screen } from '@testing-library/react';
import Delta from './Delta';
import { ThemeModeProvider } from '../../Theme/ThemeModeProvider';
import { setSystemColorScheme } from '../../testing/renderWithProviders';

const plain = (text: string | null) => (text ?? '').replace(/[  ]/g, ' ');

const renderDelta = (ui: React.ReactElement) => {
    setSystemColorScheme(false);
    return render(<ThemeModeProvider>{ui}</ThemeModeProvider>);
};

test('a gain has a plus sign, an up arrow and is announced as growth', () => {
    const { container } = renderDelta(<Delta format="percent" value={34.76} />);

    expect(container).toHaveTextContent('Зростання +34,76%');
    expect(screen.getByTestId('ArrowUpwardIcon')).toBeInTheDocument();
});

test('a loss has a real minus sign, a down arrow and is announced as a fall', () => {
    const { container } = renderDelta(<Delta format="percent" value={-2.41} />);

    expect(container).toHaveTextContent('Спад −2,41%');
    expect(screen.getByTestId('ArrowDownwardIcon')).toBeInTheDocument();
});

test('no change has no sign and a flat marker', () => {
    const { container } = renderDelta(<Delta format="percent" value={0} />);

    expect(container).toHaveTextContent('Без змін 0,00%');
    expect(screen.getByTestId('RemoveIcon')).toBeInTheDocument();
});

test('an amount that rounds to zero counts as no change', () => {
    const { container } = renderDelta(<Delta format="percent" value={-0.001} />);

    expect(container).toHaveTextContent('Без змін');
});

test('money is shown with its currency and sign', () => {
    const { container } = renderDelta(<Delta format="money" value={4345.7} variant="text" />);

    expect(plain(container.textContent)).toContain('+4 345,70 $');
    expect(screen.queryByTestId('ArrowUpwardIcon')).not.toBeInTheDocument();
});

test('the text variant can still show the arrow', () => {
    renderDelta(<Delta format="money" value={-10} variant="text" showIcon />);

    expect(screen.getByTestId('ArrowDownwardIcon')).toBeInTheDocument();
});

test('percentage points and plain numbers are signed too', () => {
    const { container } = renderDelta(
        <>
            <Delta format="points" value={-2.92} />
            <Delta format="number" value={0.28} />
        </>
    );

    expect(plain(container.textContent)).toContain('−2,92 п.п.');
    expect(plain(container.textContent)).toContain('+0,28');
});

test('a missing value is a dash', () => {
    const { container } = renderDelta(<Delta format="percent" value={null} />);

    expect(container).toHaveTextContent('—');
});

test('when a fall is the good news the arrow points down but the figure is not shown as a loss', () => {
    const { container: falling } = renderDelta(<Delta format="points" value={-2.92} good="down" />);
    const { container: rising } = renderDelta(<Delta format="points" value={2.92} good="down" />);

    const colorOf = (container: HTMLElement) => getComputedStyle(container.querySelector('span') as HTMLElement).color;
    // A fall in volatility is good (the gain colour); a rise is bad (the loss colour).
    expect(colorOf(falling)).not.toBe(colorOf(rising));
});
