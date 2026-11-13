import Box from '@mui/material/Box';
import Card from '@mui/material/Card';
import Typography from '@mui/material/Typography';
import { usePageTitle } from '../../Hooks/usePageTitle';
import PageHeader from '../Shared/PageHeader';

interface Section {
    title: string;
    paragraphs?: string[];
    items?: string[];
}

// Keep this in step with what the API really stores (User, AspNetUserLogins, Portfolios, Stocks) and with the scopes
// the app asks Google for (openid, email, profile). Google requires a privacy policy for apps that sign users in.
const SECTIONS: Section[] = [
    {
        title: 'Хто ми',
        paragraphs: [
            'Portfolio Manager — навчальний вебзастосунок для обліку інвестиційних портфелів та їх оптимізації за моделлю Марковіца.',
        ],
    },
    {
        title: 'Які дані ми отримуємо від Google',
        paragraphs: ['Вхід відбувається лише через акаунт Google. Ми запитуємо тільки базові дані профілю (openid, email, profile):'],
        items: [
            'унікальний ідентифікатор вашого акаунта Google;',
            'адреса електронної пошти (Google підтверджує, що вона ваша);',
            'ім’я, яке ви вказали в Google.',
        ],
    },
    {
        title: 'Що ми зберігаємо',
        paragraphs: [
            'Пароля в нас немає. Ми не читаємо вашу пошту, контакти, файли чи інші дані акаунта Google. Зберігаємо лише таке:',
        ],
        items: [
            'ідентифікатор акаунта Google, пошту та ім’я — щоб впізнати вас під час наступного входу;',
            'портфелі та позиції, які ви створили в застосунку;',
            'службові дані сесії: хеш токена оновлення, час його створення та закінчення;',
            'у вашому браузері: токен оновлення сесії та вибрану тему (світла чи темна).',
        ],
    },
    {
        title: 'Для чого ми їх використовуємо',
        paragraphs: [
            'Лише щоб ви могли увійти й користуватися застосунком. Дані з вашого акаунта Google ми не продаємо, не передаємо третім особам і не використовуємо для реклами.',
        ],
    },
    {
        title: 'Хто бачить ваші дані',
        paragraphs: [
            'Адміністратор застосунку бачить список користувачів: ім’я, пошту, роль, кількість портфелів і дату останньої активності. Вміст ваших портфелів адміністратор у цьому списку не бачить.',
        ],
    },
    {
        title: 'Видалення даних',
        paragraphs: [
            'Ви можете будь-коли попросити видалити свій акаунт і всі пов’язані з ним дані — зверніться до адміністратора застосунку. Доступ до застосунку для Google також можна відкликати в налаштуваннях вашого акаунта Google.',
        ],
    },
];

/** Public: Google requires a privacy policy for a sign-in with Google, and a visitor must be able to read it before signing in. */
export default function PrivacyPage() {
    usePageTitle('Політика конфіденційності');

    return (
        <>
            <PageHeader
                title="Політика конфіденційності"
                subtitle="Редакція від 12 листопада 2026 р."
            />
            <Card component="article" sx={{ p: { xs: 2, sm: 3 }, display: 'flex', flexDirection: 'column', gap: 3, maxWidth: 760 }}>
                {SECTIONS.map((section) => (
                    <Box component="section" key={section.title} sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                        <Typography component="h2" variant="h2">
                            {section.title}
                        </Typography>
                        {section.paragraphs?.map((text) => (
                            <Typography key={text} color="text.secondary">
                                {text}
                            </Typography>
                        ))}
                        {section.items && (
                            <Box component="ul" sx={{ m: 0, pl: 2.5, color: 'text.secondary', display: 'flex', flexDirection: 'column', gap: 0.5 }}>
                                {section.items.map((text) => (
                                    <li key={text}>
                                        <Typography component="span" color="inherit">
                                            {text}
                                        </Typography>
                                    </li>
                                ))}
                            </Box>
                        )}
                    </Box>
                ))}
            </Card>
        </>
    );
}
