function ModulePage({ title, description }) {
    return (
        <section className="module-page">
            <div className="page-heading-row">
                <div>
                    <h1>{title}</h1>
                    <p>{description}</p>
                </div>
            </div>

            <div className="dashboard-card module-empty-card">
                <div className="empty-symbol" aria-hidden="true">▤</div>
                <strong>No data available</strong>
                <p>
                    This module will display records from the Qubriq database
                    when its API integration is implemented.
                </p>
            </div>
        </section>
    );
}

export default ModulePage;
