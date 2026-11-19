import { createContext, useContext, useEffect, useMemo, useState } from 'react';
import { Link, NavLink, Navigate, Route, Routes, useNavigate } from 'react-router-dom';
import api from './api';

const AuthContext = createContext(null);
const useAuth = () => useContext(AuthContext);
const money = (value) => new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 }).format(value || 0);
const errorText = (error) => error?.response?.data?.message || error?.response?.data?.title || error?.message || 'Something went wrong. Please try again.';

function AuthProvider({ children }) {
  const [user, setUser] = useState(() => {
    try { return JSON.parse(localStorage.getItem('shopease_user') || 'null'); } catch { return null; }
  });
  const login = (data) => {
    localStorage.setItem('shopease_token', data.token);
    const nextUser = { id: data.userId, fullName: data.fullName, email: data.email, role: data.role };
    localStorage.setItem('shopease_user', JSON.stringify(nextUser));
    setUser(nextUser);
  };
  const logout = () => {
    localStorage.removeItem('shopease_token');
    localStorage.removeItem('shopease_user');
    setUser(null);
  };
  return <AuthContext.Provider value={{ user, login, logout }}>{children}</AuthContext.Provider>;
}

function Protected({ children, admin = false }) {
  const { user } = useAuth();
  if (!user) return <Navigate to="/login" replace />;
  if (admin && user.role !== 'Admin') return <Navigate to="/" replace />;
  return children;
}

function Header() {
  const { user, logout } = useAuth();
  const [cartCount, setCartCount] = useState(0);
  useEffect(() => {
    if (!user) { setCartCount(0); return; }
    api.get('/cart').then(({ data }) => setCartCount(data.totalItems || 0)).catch(() => setCartCount(0));
    const refresh = () => api.get('/cart').then(({ data }) => setCartCount(data.totalItems || 0)).catch(() => {});
    window.addEventListener('cart-updated', refresh);
    return () => window.removeEventListener('cart-updated', refresh);
  }, [user]);
  return <header className="topbar"><div className="nav-wrap">
    <Link to="/" className="brand"><span className="brand-mark">S</span> ShopEase</Link>
    <nav><NavLink to="/">Shop</NavLink>{user && <NavLink to="/orders">My orders</NavLink>}{user?.role === 'Admin' && <NavLink to="/admin">Admin</NavLink>}</nav>
    <div className="nav-actions"><Link className="cart-link" to="/cart">Cart <span>{cartCount}</span></Link>
      {user ? <><span className="welcome">Hi, {user.fullName.split(' ')[0]}</span><button className="text-button" onClick={logout}>Sign out</button></> : <Link className="button button-small" to="/login">Sign in</Link>}
    </div>
  </div></header>;
}

function ProductCard({ product }) {
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState('');
  const { user } = useAuth();
  const add = async () => {
    if (!user) return (window.location.href = '/login');
    setBusy(true); setMessage('');
    try {
      await api.post('/cart/items', { productId: product.id, quantity: 1 });
      setMessage('Added to cart'); window.dispatchEvent(new Event('cart-updated'));
    } catch (e) { setMessage(errorText(e)); }
    finally { setBusy(false); }
  };
  return <article className="product-card">
    <div className="product-image">{product.imageUrl ? <img src={product.imageUrl} alt={product.name} /> : <div className="image-placeholder">{product.name.slice(0, 1).toUpperCase()}</div>}
      {!product.isActive && <span className="badge badge-muted">Unavailable</span>}
    </div>
    <div className="product-info"><div className="eyebrow">{product.categoryName || 'ShopEase collection'}</div><h3>{product.name}</h3><p className="product-description">{product.description}</p>
      <div className="product-bottom"><strong>{money(product.price)}</strong><span className="stock">{product.stockQuantity > 0 ? `${product.stockQuantity} in stock` : 'Out of stock'}</span></div>
      <button className="button button-full" disabled={busy || !product.isActive || product.stockQuantity < 1} onClick={add}>{busy ? 'Adding…' : 'Add to cart'}</button>
      {message && <small className="inline-message">{message}</small>}
    </div>
  </article>;
}

function HomePage() {
  const [products, setProducts] = useState([]);
  const [categories, setCategories] = useState([]);
  const [search, setSearch] = useState('');
  const [category, setCategory] = useState('all');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  useEffect(() => {
    Promise.all([api.get('/products'), api.get('/categories')])
      .then(([p, c]) => { setProducts(p.data); setCategories(c.data.filter(x => x.isActive)); })
      .catch(e => setError(errorText(e)))
      .finally(() => setLoading(false));
  }, []);
  const filtered = products.filter(p => p.isActive && (category === 'all' || String(p.categoryId) === category) && `${p.name} ${p.description} ${p.sku}`.toLowerCase().includes(search.toLowerCase()));
  return <><section className="hero"><div className="hero-copy"><div className="eyebrow light">WELCOME TO SHopease</div><h1>Good finds.<br/><em>Better everyday.</em></h1><p>Discover useful essentials and favourites, all in one easy place.</p><a href="#products" className="button button-light">Explore products <span>→</span></a></div><div className="hero-art"><div className="hero-orbit orbit-one"/><div className="hero-orbit orbit-two"/><div className="hero-box">S<span>✦</span></div><div className="hero-caption">YOUR EVERYDAY<br/>SHOPPING PLACE</div></div></section>
    <main className="container" id="products"><div className="section-heading"><div><div className="eyebrow">THE COLLECTION</div><h2>Find your next favourite</h2><p>Thoughtful picks for everyday life.</p></div><div className="product-count">{filtered.length} products</div></div>
      <div className="filters"><input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search products or SKU…" aria-label="Search products"/><select value={category} onChange={e => setCategory(e.target.value)}><option value="all">All categories</option>{categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}</select></div>
      {loading ? <div className="empty-state">Loading products…</div> : error ? <div className="alert error">{error}</div> : filtered.length ? <div className="product-grid">{filtered.map(p => <ProductCard key={p.id} product={p}/>)}</div> : <div className="empty-state">No products found. Try another search.</div>}
    </main></>;
}

function AuthPage({ register = false }) {
  const { login } = useAuth(); const navigate = useNavigate();
  const [form, setForm] = useState({ fullName: '', email: '', password: '' });
  const [error, setError] = useState(''); const [busy, setBusy] = useState(false);
  const submit = async e => {
    e.preventDefault(); setBusy(true); setError('');
    try { const { data } = await api.post(register ? '/auth/register' : '/auth/login', form); login(data); navigate(data.role === 'Admin' ? '/admin' : '/'); }
    catch (e) { setError(errorText(e)); } finally { setBusy(false); }
  };
  return <main className="auth-shell"><div className="auth-card"><div className="eyebrow">{register ? 'JOIN SHopease' : 'WELCOME BACK'}</div><h1>{register ? 'Create your account' : 'Sign in to ShopEase'}</h1><p>{register ? 'A few details and you are ready to shop.' : 'Your next great find is just around the corner.'}</p>
    {error && <div className="alert error">{error}</div>}<form onSubmit={submit} className="form-stack">
      {register && <label>Full name<input required minLength="2" maxLength="120" value={form.fullName} onChange={e => setForm({ ...form, fullName: e.target.value })}/></label>}
      <label>Email address<input required type="email" value={form.email} onChange={e => setForm({ ...form, email: e.target.value })}/></label>
      <label>Password<input required minLength={register ? 8 : 1} type="password" value={form.password} onChange={e => setForm({ ...form, password: e.target.value })}/>{register && <small>Use at least 8 characters.</small>}</label>
      <button className="button button-full" disabled={busy}>{busy ? 'Please wait…' : register ? 'Create account' : 'Sign in'}</button>
    </form><div className="auth-switch">{register ? 'Already have an account?' : 'New to ShopEase?'} <Link to={register ? '/login' : '/register'}>{register ? 'Sign in' : 'Create an account'}</Link></div>
  </div></main>;
}

function CartPage() {
  const [cart, setCart] = useState(null); const [error, setError] = useState(''); const [busy, setBusy] = useState(false); const [shipping, setShipping] = useState({ shippingName: '', shippingAddress: '', shippingCity: '', shippingPostalCode: '', phone: '' });
  const navigate = useNavigate();
  const load = () => api.get('/cart').then(r => setCart(r.data)).catch(e => setError(errorText(e)));
  useEffect(() => { load(); }, []);
  const update = async (id, quantity) => { try { await api.put(`/cart/items/${id}`, { quantity }); await load(); window.dispatchEvent(new Event('cart-updated')); } catch (e) { setError(errorText(e)); } };
  const remove = async id => { try { await api.delete(`/cart/items/${id}`); await load(); window.dispatchEvent(new Event('cart-updated')); } catch (e) { setError(errorText(e)); } };
  const checkout = async e => { e.preventDefault(); setBusy(true); setError(''); try { await api.post('/orders/checkout', shipping); window.dispatchEvent(new Event('cart-updated')); navigate('/orders'); } catch (e) { setError(errorText(e)); } finally { setBusy(false); } };
  if (!cart) return <main className="container page-space"><div className="empty-state">Loading cart…</div></main>;
  return <main className="container page-space"><div className="page-title"><div className="eyebrow">YOUR BAG</div><h1>Shopping cart</h1><p>Review your items before checkout.</p></div>{error && <div className="alert error">{error}</div>}
    {!cart.items.length ? <div className="empty-state"><h3>Your cart is empty</h3><p>Find something you love and add it here.</p><Link className="button" to="/">Browse products</Link></div> : <div className="checkout-layout"><section className="panel cart-panel"><div className="panel-heading"><h2>Your items</h2><span>{cart.totalItems} items</span></div>{cart.items.map(item => <div className="cart-row" key={item.productId}><div className="cart-thumb">{item.imageUrl ? <img src={item.imageUrl} alt=""/> : item.name.slice(0,1)}</div><div className="cart-product"><strong>{item.name}</strong><small>{item.sku}</small><button className="link-button" onClick={() => remove(item.productId)}>Remove</button></div><div className="quantity-control"><button onClick={() => update(item.productId, Math.max(1, item.quantity - 1))}>−</button><span>{item.quantity}</span><button disabled={item.quantity >= item.availableStock} onClick={() => update(item.productId, item.quantity + 1)}>+</button></div><strong className="line-total">{money(item.lineTotal)}</strong></div>)}<div className="subtotal-line"><span>Subtotal</span><strong>{money(cart.subtotal)}</strong></div><p className="muted small-text">Shipping and taxes are not charged in this demo. Payment method: Cash on Delivery.</p></section>
      <section className="panel"><div className="panel-heading"><h2>Delivery details</h2></div><form onSubmit={checkout} className="form-stack"><label>Full name<input required maxLength="120" value={shipping.shippingName} onChange={e => setShipping({ ...shipping, shippingName: e.target.value })}/></label><label>Address<input required maxLength="300" value={shipping.shippingAddress} onChange={e => setShipping({ ...shipping, shippingAddress: e.target.value })}/></label><div className="form-two"><label>City<input required value={shipping.shippingCity} onChange={e => setShipping({ ...shipping, shippingCity: e.target.value })}/></label><label>PIN code<input required value={shipping.shippingPostalCode} onChange={e => setShipping({ ...shipping, shippingPostalCode: e.target.value })}/></label></div><label>Phone<input required type="tel" value={shipping.phone} onChange={e => setShipping({ ...shipping, phone: e.target.value })}/></label><div className="total-line"><span>Order total</span><strong>{money(cart.subtotal)}</strong></div><button className="button button-full" disabled={busy || !cart.items.length}>{busy ? 'Placing order…' : 'Place order · Cash on Delivery'}</button></form></section></div>}
  </main>;
}

function OrdersPage() {
  const [orders, setOrders] = useState([]); const [error, setError] = useState('');
  useEffect(() => { api.get('/orders/my').then(r => setOrders(r.data)).catch(e => setError(errorText(e))); }, []);
  return <main className="container page-space"><div className="page-title"><div className="eyebrow">YOUR ACCOUNT</div><h1>Order history</h1><p>Track the status of your ShopEase orders.</p></div>{error && <div className="alert error">{error}</div>}{!orders.length && !error ? <div className="empty-state">No orders yet. <Link to="/">Start shopping</Link></div> : <div className="order-list">{orders.map(o => <article className="panel order-card" key={o.id}><div className="order-head"><div><small>ORDER NUMBER</small><strong>{o.orderNumber}</strong><span>{new Date(o.orderDate).toLocaleString()}</span></div><span className={`status status-${o.status.toLowerCase()}`}>{o.status}</span></div><div className="order-items">{o.items.map((i, index) => <div key={index}><span>{i.productName} × {i.quantity}</span><strong>{money(i.lineTotal)}</strong></div>)}</div><div className="subtotal-line"><span>Cash on Delivery</span><strong>{money(o.totalAmount)}</strong></div><p className="muted small-text">Deliver to: {o.shippingName}, {o.shippingAddress}, {o.shippingCity} - {o.shippingPostalCode}</p></article>)}</div>}</main>;
}

function AdminPage() {
  const [products, setProducts] = useState([]); const [categories, setCategories] = useState([]); const [orders, setOrders] = useState([]); const [error, setError] = useState(''); const [notice, setNotice] = useState('');
  const [product, setProduct] = useState({ name: '', description: '', sku: '', price: '', stockQuantity: '', categoryId: '', imageUrl: '' });
  const [category, setCategory] = useState({ name: '', description: '' });
  const load = async () => { try { const [p,c,o] = await Promise.all([api.get('/products'), api.get('/categories'), api.get('/orders/admin')]); setProducts(p.data); setCategories(c.data); setOrders(o.data); } catch (e) { setError(errorText(e)); } };
  useEffect(() => { load(); }, []);
  const addProduct = async e => { e.preventDefault(); setError(''); setNotice(''); try { await api.post('/products', { ...product, price: Number(product.price), stockQuantity: Number(product.stockQuantity), categoryId: Number(product.categoryId), imageUrl: product.imageUrl || null }); setProduct({ name:'', description:'', sku:'', price:'', stockQuantity:'', categoryId:'', imageUrl:'' }); setNotice('Product created.'); await load(); } catch (e) { setError(errorText(e)); } };
  const addCategory = async e => { e.preventDefault(); setError(''); try { await api.post('/categories', category); setCategory({ name:'', description:'' }); setNotice('Category created.'); await load(); } catch (e) { setError(errorText(e)); } };
  const changeStatus = async (id, status) => { try { await api.patch(`/orders/admin/${id}/status`, { status }); setNotice(`Order status changed to ${status}.`); await load(); } catch (e) { setError(errorText(e)); } };
  return <main className="container page-space"><div className="page-title"><div className="eyebrow">STORE MANAGEMENT</div><h1>Admin dashboard</h1><p>Manage your catalogue and customer orders.</p></div>{error && <div className="alert error">{error}</div>}{notice && <div className="alert success">{notice}</div>}
    <div className="admin-grid"><section className="panel"><div className="panel-heading"><h2>Add product</h2></div><form onSubmit={addProduct} className="form-stack"><label>Product name<input required value={product.name} onChange={e=>setProduct({...product,name:e.target.value})}/></label><label>Description<input value={product.description} onChange={e=>setProduct({...product,description:e.target.value})}/></label><div className="form-two"><label>SKU<input required value={product.sku} onChange={e=>setProduct({...product,sku:e.target.value})}/></label><label>Price (₹)<input required type="number" min="0.01" step="0.01" value={product.price} onChange={e=>setProduct({...product,price:e.target.value})}/></label></div><div className="form-two"><label>Stock<input required type="number" min="0" value={product.stockQuantity} onChange={e=>setProduct({...product,stockQuantity:e.target.value})}/></label><label>Category<select required value={product.categoryId} onChange={e=>setProduct({...product,categoryId:e.target.value})}><option value="">Choose…</option>{categories.map(c=><option key={c.id} value={c.id}>{c.name}</option>)}</select></label></div><label>Image URL (optional)<input value={product.imageUrl} onChange={e=>setProduct({...product,imageUrl:e.target.value})}/></label><button className="button">Save product</button></form></section>
    <section className="panel"><div className="panel-heading"><h2>Add category</h2></div><form onSubmit={addCategory} className="form-stack"><label>Category name<input required value={category.name} onChange={e=>setCategory({...category,name:e.target.value})}/></label><label>Description<input value={category.description} onChange={e=>setCategory({...category,description:e.target.value})}/></label><button className="button">Save category</button></form><div className="mini-list"><h3>Current categories</h3>{categories.map(c=><div key={c.id}><span>{c.name}</span><small>{c.isActive ? 'Active' : 'Inactive'}</small></div>)}</div></section></div>
    <section className="panel table-panel"><div className="panel-heading"><h2>Products</h2><span>{products.length} total</span></div><div className="table-scroll"><table><thead><tr><th>Product</th><th>SKU</th><th>Price</th><th>Stock</th><th>Category</th></tr></thead><tbody>{products.map(p=><tr key={p.id}><td>{p.name}</td><td>{p.sku}</td><td>{money(p.price)}</td><td>{p.stockQuantity}</td><td>{p.categoryName}</td></tr>)}</tbody></table></div></section>
    <section className="panel table-panel"><div className="panel-heading"><h2>Customer orders</h2><span>{orders.length} total</span></div><div className="table-scroll"><table><thead><tr><th>Order</th><th>Customer</th><th>Date</th><th>Total</th><th>Status</th><th>Update</th></tr></thead><tbody>{orders.map(o=><tr key={o.id}><td>{o.orderNumber}</td><td>{o.customerEmail}</td><td>{new Date(o.orderDate).toLocaleDateString()}</td><td>{money(o.totalAmount)}</td><td><span className={`status status-${o.status.toLowerCase()}`}>{o.status}</span></td><td><select value={o.status} onChange={e=>changeStatus(o.id,e.target.value)}><option>Pending</option><option>Confirmed</option><option>Shipped</option><option>Delivered</option><option>Cancelled</option></select></td></tr>)}</tbody></table></div></section>
  </main>;
}

function Footer() { return <footer className="footer"><div className="container footer-inner"><Link to="/" className="brand"><span className="brand-mark">S</span> ShopEase</Link><span>Everyday shopping, made easy.</span><span>© {new Date().getFullYear()} ShopEase</span></div></footer>; }

export default function App() {
  return <AuthProvider><div className="app-shell"><Header/><Routes>
    <Route path="/" element={<HomePage/>}/><Route path="/login" element={<AuthPage/>}/><Route path="/register" element={<AuthPage register/>}/>
    <Route path="/cart" element={<Protected><CartPage/></Protected>}/><Route path="/orders" element={<Protected><OrdersPage/></Protected>}/>
    <Route path="/admin" element={<Protected admin><AdminPage/></Protected>}/><Route path="*" element={<Navigate to="/" replace/>}/>
  </Routes><Footer/></div></AuthProvider>;
}
